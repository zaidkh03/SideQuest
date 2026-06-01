using Microsoft.EntityFrameworkCore;
using SideQuest.Contracts;
using SideQuest.Data;
using SideQuest.Models;

namespace SideQuest.Services
{
    public interface IApplicationService
    {
        Task<ServiceResult<IReadOnlyList<JobApplicationResponse>>> GetWorkerApplicationsAsync(string workerUserId);

        Task<ServiceResult<IReadOnlyList<JobApplicationResponse>>> GetJobApplicationsAsync(string employerUserId, int jobId);

        Task<ServiceResult<JobApplicationResponse>> ApplyAsync(string workerUserId, int jobId, CreateApplicationRequest request);

        Task<ServiceResult<JobApplicationResponse>> WithdrawAsync(string workerUserId, int applicationId);

        Task<ServiceResult<JobApplicationResponse>> AcceptAsync(string employerUserId, int applicationId);

        Task<ServiceResult<JobApplicationResponse>> RejectAsync(string employerUserId, int applicationId);
    }

    public class ApplicationService : IApplicationService
    {
        private readonly AppDbContext _context;

        public ApplicationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<IReadOnlyList<JobApplicationResponse>>> GetWorkerApplicationsAsync(string workerUserId)
        {
            var applications = await GetApplicationQuery()
                .Where(application => application.WorkerId == workerUserId)
                .OrderByDescending(application => application.AppliedAt)
                .ToListAsync();

            return ServiceResult<IReadOnlyList<JobApplicationResponse>>.Success(
                applications.Select(application => application.ToResponse()).ToList());
        }

        public async Task<ServiceResult<IReadOnlyList<JobApplicationResponse>>> GetJobApplicationsAsync(string employerUserId, int jobId)
        {
            var job = await _context.Jobs
                .Include(existingJob => existingJob.Company)
                .FirstOrDefaultAsync(existingJob => existingJob.Id == jobId);

            if (job is null)
            {
                return ServiceResult<IReadOnlyList<JobApplicationResponse>>.NotFound("Job was not found.");
            }

            if (job.Company.UserId != employerUserId)
            {
                return ServiceResult<IReadOnlyList<JobApplicationResponse>>.Forbidden("Only the job owner can view applications.");
            }

            var applications = await GetApplicationQuery()
                .Where(application => application.JobId == jobId)
                .OrderByDescending(application => application.AppliedAt)
                .ToListAsync();

            return ServiceResult<IReadOnlyList<JobApplicationResponse>>.Success(
                applications.Select(application => application.ToResponse()).ToList());
        }

        public async Task<ServiceResult<JobApplicationResponse>> ApplyAsync(string workerUserId, int jobId, CreateApplicationRequest request)
        {
            if (!await _context.WorkerProfiles.AnyAsync(workerProfile => workerProfile.UserId == workerUserId))
            {
                return ServiceResult<JobApplicationResponse>.NotFound("Worker profile is required before applying for jobs.");
            }

            var job = await _context.Jobs
                .Include(existingJob => existingJob.Company)
                .Include(existingJob => existingJob.Applications)
                .FirstOrDefaultAsync(existingJob => existingJob.Id == jobId);

            if (job is null || job.Status != JobStatus.Open)
            {
                return ServiceResult<JobApplicationResponse>.NotFound("Open job was not found.");
            }

            if (job.Company.UserId == workerUserId)
            {
                return ServiceResult<JobApplicationResponse>.Conflict("Employers cannot apply to their own jobs.");
            }

            if (job.Applications.Any(application => application.WorkerId == workerUserId))
            {
                return ServiceResult<JobApplicationResponse>.Conflict("You have already applied to this job.");
            }

            var application = new JobApplication
            {
                JobId = job.Id,
                WorkerId = workerUserId,
                CoverLetter = request.CoverLetter.Trim(),
                Status = ApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow
            };

            _context.JobApplications.Add(application);
            AddNotification(
                job.Company.UserId,
                "New application received",
                $"A worker applied to {job.Title}.",
                "ApplicationSubmitted");

            await _context.SaveChangesAsync();

            var savedApplication = await GetApplicationQuery()
                .FirstAsync(saved => saved.Id == application.Id);

            return ServiceResult<JobApplicationResponse>.Created(savedApplication.ToResponse());
        }

        public async Task<ServiceResult<JobApplicationResponse>> WithdrawAsync(string workerUserId, int applicationId)
        {
            var application = await GetApplicationQuery()
                .FirstOrDefaultAsync(existingApplication => existingApplication.Id == applicationId);

            if (application is null)
            {
                return ServiceResult<JobApplicationResponse>.NotFound("Application was not found.");
            }

            if (application.WorkerId != workerUserId)
            {
                return ServiceResult<JobApplicationResponse>.Forbidden("Only the applicant can withdraw this application.");
            }

            if (application.Status != ApplicationStatus.Pending)
            {
                return ServiceResult<JobApplicationResponse>.Conflict("Only pending applications can be withdrawn.");
            }

            application.Status = ApplicationStatus.Withdrawn;
            AddNotification(
                application.Job.Company.UserId,
                "Application withdrawn",
                $"An application for {application.Job.Title} was withdrawn.",
                "ApplicationWithdrawn");

            await _context.SaveChangesAsync();

            return ServiceResult<JobApplicationResponse>.Success(application.ToResponse());
        }

        public async Task<ServiceResult<JobApplicationResponse>> AcceptAsync(string employerUserId, int applicationId)
        {
            var application = await GetApplicationQuery()
                .Include(existingApplication => existingApplication.Job.Assignments)
                .FirstOrDefaultAsync(existingApplication => existingApplication.Id == applicationId);

            if (application is null)
            {
                return ServiceResult<JobApplicationResponse>.NotFound("Application was not found.");
            }

            if (application.Job.Company.UserId != employerUserId)
            {
                return ServiceResult<JobApplicationResponse>.Forbidden("Only the job owner can accept applications.");
            }

            if (application.Status != ApplicationStatus.Pending)
            {
                return ServiceResult<JobApplicationResponse>.Conflict("Only pending applications can be accepted.");
            }

            if (application.Job.Assignments.Count >= application.Job.WorkersNeeded)
            {
                return ServiceResult<JobApplicationResponse>.Conflict("This job already has the required number of workers.");
            }

            application.Status = ApplicationStatus.Accepted;

            var agreedRate = application.Job.BudgetType == BudgetType.Fixed
                ? decimal.Round(application.Job.FixedBudget / application.Job.WorkersNeeded, 2)
                : application.Job.HourlyRate;

            if (!application.Job.Assignments.Any(assignment => assignment.WorkerId == application.WorkerId))
            {
                application.Job.Assignments.Add(new JobAssignment
                {
                    JobId = application.JobId,
                    WorkerId = application.WorkerId,
                    AgreedRate = agreedRate
                });
            }

            if (application.Job.Assignments.Count >= application.Job.WorkersNeeded)
            {
                application.Job.Status = JobStatus.InProgress;
            }

            AddNotification(
                application.WorkerId,
                "Application accepted",
                $"Your application for {application.Job.Title} was accepted.",
                "ApplicationAccepted");

            await _context.SaveChangesAsync();

            return ServiceResult<JobApplicationResponse>.Success(application.ToResponse());
        }

        public async Task<ServiceResult<JobApplicationResponse>> RejectAsync(string employerUserId, int applicationId)
        {
            var application = await GetApplicationQuery()
                .FirstOrDefaultAsync(existingApplication => existingApplication.Id == applicationId);

            if (application is null)
            {
                return ServiceResult<JobApplicationResponse>.NotFound("Application was not found.");
            }

            if (application.Job.Company.UserId != employerUserId)
            {
                return ServiceResult<JobApplicationResponse>.Forbidden("Only the job owner can reject applications.");
            }

            if (application.Status != ApplicationStatus.Pending)
            {
                return ServiceResult<JobApplicationResponse>.Conflict("Only pending applications can be rejected.");
            }

            application.Status = ApplicationStatus.Rejected;
            AddNotification(
                application.WorkerId,
                "Application rejected",
                $"Your application for {application.Job.Title} was not accepted.",
                "ApplicationRejected");

            await _context.SaveChangesAsync();

            return ServiceResult<JobApplicationResponse>.Success(application.ToResponse());
        }

        private IQueryable<JobApplication> GetApplicationQuery()
        {
            return _context.JobApplications
                .Include(application => application.Job)
                    .ThenInclude(job => job.Company)
                .Include(application => application.Worker);
        }

        private void AddNotification(string userId, string title, string message, string type)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
