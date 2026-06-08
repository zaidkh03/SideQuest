using Microsoft.EntityFrameworkCore;
using SideQuest.Contracts;
using SideQuest.Data;
using SideQuest.Models;

namespace SideQuest.Services
{
    public interface IJobService
    {
        Task<ServiceResult<IReadOnlyList<JobResponse>>> GetOpenJobsAsync(JobQueryParameters query);

        Task<ServiceResult<IReadOnlyList<JobResponse>>> GetEmployerJobsAsync(string employerUserId);

        Task<ServiceResult<IReadOnlyList<JobResponse>>> GetAdminJobsAsync(JobQueryParameters query);

        Task<ServiceResult<JobResponse>> GetJobAsync(int jobId, string? requesterUserId, bool isAdmin);

        Task<ServiceResult<JobResponse>> CreateJobAsync(string employerUserId, UpsertJobRequest request);

        Task<ServiceResult<JobResponse>> UpdateJobAsync(string employerUserId, int jobId, UpsertJobRequest request);

        Task<ServiceResult<JobResponse>> PublishJobAsync(string employerUserId, int jobId);

        Task<ServiceResult<JobResponse>> ApproveJobCommissionAsync(string adminUserId, int jobId);

        Task<ServiceResult<JobResponse>> RequestCommissionUpdateAsync(string adminUserId, int jobId, JobCommissionUpdateRequest request);

        Task<ServiceResult<JobResponse>> CancelJobAsync(string employerUserId, int jobId);

        Task<ServiceResult<JobResponse>> CloseJobAsync(string employerUserId, int jobId);
    }

    public class JobService : IJobService
    {
        private const decimal MinimumCommissionRate = 10m;

        private readonly AppDbContext _context;

        public JobService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<IReadOnlyList<JobResponse>>> GetOpenJobsAsync(JobQueryParameters query)
        {
            var jobs = await ApplyJobFilters(GetJobQuery().Where(job => job.Status == JobStatus.Open), query, forceOpenStatus: true)
                .ToListAsync();

            return ServiceResult<IReadOnlyList<JobResponse>>.Success(jobs.Select(job => job.ToResponse()).ToList());
        }

        public async Task<ServiceResult<IReadOnlyList<JobResponse>>> GetEmployerJobsAsync(string employerUserId)
        {
            var company = await _context.CompanyProfiles
                .FirstOrDefaultAsync(companyProfile => companyProfile.UserId == employerUserId);

            if (company is null)
            {
                return ServiceResult<IReadOnlyList<JobResponse>>.NotFound("Company profile was not found.");
            }

            var jobs = await GetJobQuery()
                .Where(job => job.CompanyId == company.Id)
                .OrderByDescending(job => job.CreatedAt)
                .ToListAsync();

            return ServiceResult<IReadOnlyList<JobResponse>>.Success(jobs.Select(job => job.ToResponse()).ToList());
        }

        public async Task<ServiceResult<IReadOnlyList<JobResponse>>> GetAdminJobsAsync(JobQueryParameters query)
        {
            var jobs = await ApplyJobFilters(GetJobQuery(), query, forceOpenStatus: false)
                .ToListAsync();

            return ServiceResult<IReadOnlyList<JobResponse>>.Success(jobs.Select(job => job.ToResponse()).ToList());
        }

        public async Task<ServiceResult<JobResponse>> GetJobAsync(int jobId, string? requesterUserId, bool isAdmin)
        {
            var job = await GetJobQuery()
                .FirstOrDefaultAsync(job => job.Id == jobId);

            if (job is null)
            {
                return ServiceResult<JobResponse>.NotFound("Job was not found.");
            }

            if (job.Status == JobStatus.Open || isAdmin || job.Company.UserId == requesterUserId)
            {
                return ServiceResult<JobResponse>.Success(job.ToResponse());
            }

            return ServiceResult<JobResponse>.NotFound("Job was not found.");
        }

        public async Task<ServiceResult<JobResponse>> CreateJobAsync(string employerUserId, UpsertJobRequest request)
        {
            var company = await _context.CompanyProfiles
                .FirstOrDefaultAsync(companyProfile => companyProfile.UserId == employerUserId);

            if (company is null)
            {
                return ServiceResult<JobResponse>.NotFound("Company profile is required before posting jobs.");
            }

            var validation = await ValidateJobRequestAsync(request);
            if (validation is not null)
            {
                return validation;
            }

            var job = new Job
            {
                CompanyId = company.Id,
                CategoryId = request.CategoryId,
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                BudgetType = BudgetType.Hourly,
                FixedBudget = 0,
                HourlyRate = request.HourlyRate,
                HoursPerDay = request.HoursPerDay,
                DurationDays = request.DurationDays,
                WorkersNeeded = request.WorkersNeeded,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = JobStatus.PendingApproval,
                OfferedCommissionRate = request.OfferedCommissionRate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Jobs.Add(job);
            AddNotification(
                company.UserId,
                "Job sent for review",
                $"{job.Title} was submitted for admin commission review.",
                "JobPendingApproval");

            await _context.SaveChangesAsync();

            var savedJob = await GetJobQuery().FirstAsync(saved => saved.Id == job.Id);
            return ServiceResult<JobResponse>.Created(savedJob.ToResponse());
        }

        public async Task<ServiceResult<JobResponse>> UpdateJobAsync(string employerUserId, int jobId, UpsertJobRequest request)
        {
            var job = await GetJobQuery()
                .FirstOrDefaultAsync(existingJob => existingJob.Id == jobId);

            if (job is null)
            {
                return ServiceResult<JobResponse>.NotFound("Job was not found.");
            }

            if (job.Company.UserId != employerUserId)
            {
                return ServiceResult<JobResponse>.Forbidden("Only the job owner can update this job.");
            }

            if (job.Status is JobStatus.Completed or JobStatus.Cancelled or JobStatus.InProgress or JobStatus.WaitingForReview)
            {
                return ServiceResult<JobResponse>.Conflict("Jobs with active or completed work cannot be updated.");
            }

            var validation = await ValidateJobRequestAsync(request);
            if (validation is not null)
            {
                return validation;
            }

            var requiredRate = job.RequiredCommissionRate;

            job.CategoryId = request.CategoryId;
            job.Title = request.Title.Trim();
            job.Description = request.Description.Trim();
            job.BudgetType = BudgetType.Hourly;
            job.FixedBudget = 0;
            job.HourlyRate = request.HourlyRate;
            job.HoursPerDay = request.HoursPerDay;
            job.DurationDays = request.DurationDays;
            job.WorkersNeeded = request.WorkersNeeded;
            job.StartDate = request.StartDate;
            job.EndDate = request.EndDate;
            job.OfferedCommissionRate = request.OfferedCommissionRate;

            if (requiredRate.HasValue && request.OfferedCommissionRate >= requiredRate.Value)
            {
                job.Status = JobStatus.Open;
                job.ApprovedCommissionRate = request.OfferedCommissionRate;
                job.RequiredCommissionRate = null;
                job.CommissionReviewNote = $"Company accepted the requested {requiredRate.Value:0.##}% platform commission.";
                job.CommissionReviewedAt = DateTime.UtcNow;
                AddNotification(
                    employerUserId,
                    "Job approved",
                    $"{job.Title} now matches the requested commission and is open to workers.",
                    "JobApproved");
            }
            else
            {
                job.Status = JobStatus.PendingApproval;
                job.ApprovedCommissionRate = null;
                job.CommissionReviewNote = requiredRate.HasValue
                    ? $"Admin requested at least {requiredRate.Value:0.##}% platform commission."
                    : null;
                job.CommissionReviewedAt = null;
            }

            await _context.SaveChangesAsync();

            var savedJob = await GetJobQuery().FirstAsync(saved => saved.Id == job.Id);
            return ServiceResult<JobResponse>.Success(savedJob.ToResponse());
        }

        public async Task<ServiceResult<JobResponse>> PublishJobAsync(string employerUserId, int jobId)
        {
            var job = await GetJobQuery()
                .FirstOrDefaultAsync(existingJob => existingJob.Id == jobId);

            if (job is null)
            {
                return ServiceResult<JobResponse>.NotFound("Job was not found.");
            }

            if (job.Company.UserId != employerUserId)
            {
                return ServiceResult<JobResponse>.Forbidden("Only the job owner can submit this job.");
            }

            if (job.Status == JobStatus.Draft)
            {
                job.Status = JobStatus.PendingApproval;
                await _context.SaveChangesAsync();
                return ServiceResult<JobResponse>.Success(job.ToResponse());
            }

            return ServiceResult<JobResponse>.Conflict("Jobs are sent for admin review as soon as they are submitted.");
        }

        public async Task<ServiceResult<JobResponse>> ApproveJobCommissionAsync(string adminUserId, int jobId)
        {
            var job = await GetJobQuery()
                .FirstOrDefaultAsync(existingJob => existingJob.Id == jobId);

            if (job is null)
            {
                return ServiceResult<JobResponse>.NotFound("Job was not found.");
            }

            if (job.Status is not JobStatus.PendingApproval and not JobStatus.NeedsCommissionUpdate)
            {
                return ServiceResult<JobResponse>.Conflict("Only jobs waiting on commission review can be approved.");
            }

            if (job.OfferedCommissionRate < MinimumCommissionRate)
            {
                return ServiceResult<JobResponse>.Validation(
                    nameof(job.OfferedCommissionRate),
                    $"Platform commission must be at least {MinimumCommissionRate:0.##}%.");
            }

            job.Status = JobStatus.Open;
            job.ApprovedCommissionRate = job.OfferedCommissionRate;
            job.RequiredCommissionRate = null;
            job.CommissionReviewNote = "Approved for publication.";
            job.CommissionReviewedAt = DateTime.UtcNow;
            job.CommissionReviewedByAdminId = adminUserId;

            AddNotification(
                job.Company.UserId,
                "Job approved",
                $"{job.Title} was approved and is now open to workers.",
                "JobApproved");

            await _context.SaveChangesAsync();
            return ServiceResult<JobResponse>.Success(job.ToResponse());
        }

        public async Task<ServiceResult<JobResponse>> RequestCommissionUpdateAsync(string adminUserId, int jobId, JobCommissionUpdateRequest request)
        {
            var job = await GetJobQuery()
                .FirstOrDefaultAsync(existingJob => existingJob.Id == jobId);

            if (job is null)
            {
                return ServiceResult<JobResponse>.NotFound("Job was not found.");
            }

            if (job.Status is not JobStatus.PendingApproval and not JobStatus.NeedsCommissionUpdate)
            {
                return ServiceResult<JobResponse>.Conflict("Only jobs waiting on commission review can be negotiated.");
            }

            if (request.RequiredCommissionRate < MinimumCommissionRate)
            {
                return ServiceResult<JobResponse>.Validation(
                    nameof(request.RequiredCommissionRate),
                    $"Required commission must be at least {MinimumCommissionRate:0.##}%.");
            }

            if (request.RequiredCommissionRate <= job.OfferedCommissionRate)
            {
                return ServiceResult<JobResponse>.Conflict("Approve the job when the offered commission already meets your requirement.");
            }

            job.Status = JobStatus.NeedsCommissionUpdate;
            job.RequiredCommissionRate = request.RequiredCommissionRate;
            job.ApprovedCommissionRate = null;
            job.CommissionReviewNote = request.Note.Trim();
            job.CommissionReviewedAt = DateTime.UtcNow;
            job.CommissionReviewedByAdminId = adminUserId;

            AddNotification(
                job.Company.UserId,
                "Commission update requested",
                $"{job.Title} needs at least {request.RequiredCommissionRate:0.##}% platform commission before approval.",
                "JobNeedsCommissionUpdate");

            await _context.SaveChangesAsync();
            return ServiceResult<JobResponse>.Success(job.ToResponse());
        }

        public async Task<ServiceResult<JobResponse>> CancelJobAsync(string employerUserId, int jobId)
        {
            var job = await GetJobQuery()
                .FirstOrDefaultAsync(existingJob => existingJob.Id == jobId);

            if (job is null)
            {
                return ServiceResult<JobResponse>.NotFound("Job was not found.");
            }

            if (job.Company.UserId != employerUserId)
            {
                return ServiceResult<JobResponse>.Forbidden("Only the job owner can cancel this job.");
            }

            if (job.Status is JobStatus.Completed or JobStatus.Cancelled)
            {
                return ServiceResult<JobResponse>.Conflict("This job is already closed.");
            }

            job.Status = JobStatus.Cancelled;
            await _context.SaveChangesAsync();

            return ServiceResult<JobResponse>.Success(job.ToResponse());
        }

        public async Task<ServiceResult<JobResponse>> CloseJobAsync(string employerUserId, int jobId)
        {
            var job = await GetJobQuery()
                .FirstOrDefaultAsync(existingJob => existingJob.Id == jobId);

            if (job is null)
            {
                return ServiceResult<JobResponse>.NotFound("Job was not found.");
            }

            if (job.Company.UserId != employerUserId)
            {
                return ServiceResult<JobResponse>.Forbidden("Only the job owner can close this job.");
            }

            if (job.Status is JobStatus.Completed or JobStatus.Cancelled)
            {
                return ServiceResult<JobResponse>.Conflict("This job is already closed.");
            }

            if (job.Assignments.Any() && job.Assignments.Any(assignment => !assignment.IsCompleted))
            {
                return ServiceResult<JobResponse>.Conflict("All assignments must be completed before closing the job.");
            }

            job.Status = JobStatus.Completed;
            await _context.SaveChangesAsync();

            return ServiceResult<JobResponse>.Success(job.ToResponse());
        }

        private IQueryable<Job> GetJobQuery()
        {
            return _context.Jobs
                .Include(job => job.Company)
                .Include(job => job.Category)
                .Include(job => job.Assignments)
                .OrderByDescending(job => job.CreatedAt);
        }

        private static IQueryable<Job> ApplyJobFilters(IQueryable<Job> queryable, JobQueryParameters query, bool forceOpenStatus)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                queryable = queryable.Where(job => job.Title.Contains(search) || job.Description.Contains(search));
            }

            if (query.CategoryId.HasValue)
            {
                queryable = queryable.Where(job => job.CategoryId == query.CategoryId.Value);
            }

            if (query.BudgetType.HasValue)
            {
                queryable = queryable.Where(job => job.BudgetType == query.BudgetType.Value);
            }

            if (!forceOpenStatus && query.Status.HasValue)
            {
                queryable = queryable.Where(job => job.Status == query.Status.Value);
            }

            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 200);

            return queryable.Skip((page - 1) * pageSize).Take(pageSize);
        }

        private async Task<ServiceResult<JobResponse>?> ValidateJobRequestAsync(UpsertJobRequest request)
        {
            if (!await _context.Categories.AnyAsync(category => category.Id == request.CategoryId && category.IsActive))
            {
                return ServiceResult<JobResponse>.Validation(nameof(request.CategoryId), "An active category is required.");
            }

            if (request.StartDate == default || request.EndDate == default || request.EndDate <= request.StartDate)
            {
                return ServiceResult<JobResponse>.Validation(nameof(request.EndDate), "End date must be after start date.");
            }

            if (request.WorkersNeeded < 1)
            {
                return ServiceResult<JobResponse>.Validation(nameof(request.WorkersNeeded), "At least one worker is required.");
            }

            if (request.HourlyRate <= 0)
            {
                return ServiceResult<JobResponse>.Validation(nameof(request.HourlyRate), "Hourly rate must be greater than zero.");
            }

            if (request.HoursPerDay <= 0 || request.HoursPerDay > 24)
            {
                return ServiceResult<JobResponse>.Validation(nameof(request.HoursPerDay), "Hours per day must be between 0.25 and 24.");
            }

            if (request.DurationDays < 1)
            {
                return ServiceResult<JobResponse>.Validation(nameof(request.DurationDays), "Number of days must be at least one.");
            }

            if (request.OfferedCommissionRate < MinimumCommissionRate || request.OfferedCommissionRate > 100)
            {
                return ServiceResult<JobResponse>.Validation(
                    nameof(request.OfferedCommissionRate),
                    $"Platform commission must be between {MinimumCommissionRate:0.##}% and 100%.");
            }

            return null;
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
