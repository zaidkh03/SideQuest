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

        Task<ServiceResult<JobResponse>> CancelJobAsync(string employerUserId, int jobId);

        Task<ServiceResult<JobResponse>> CloseJobAsync(string employerUserId, int jobId);
    }

    public class JobService : IJobService
    {
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
                BudgetType = request.BudgetType,
                FixedBudget = request.BudgetType == BudgetType.Fixed ? request.FixedBudget : 0,
                HourlyRate = request.BudgetType == BudgetType.Hourly ? request.HourlyRate : 0,
                WorkersNeeded = request.WorkersNeeded,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = JobStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            _context.Jobs.Add(job);
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

            if (job.Status is JobStatus.Completed or JobStatus.Cancelled)
            {
                return ServiceResult<JobResponse>.Conflict("Completed or cancelled jobs cannot be updated.");
            }

            var validation = await ValidateJobRequestAsync(request);
            if (validation is not null)
            {
                return validation;
            }

            job.CategoryId = request.CategoryId;
            job.Title = request.Title.Trim();
            job.Description = request.Description.Trim();
            job.BudgetType = request.BudgetType;
            job.FixedBudget = request.BudgetType == BudgetType.Fixed ? request.FixedBudget : 0;
            job.HourlyRate = request.BudgetType == BudgetType.Hourly ? request.HourlyRate : 0;
            job.WorkersNeeded = request.WorkersNeeded;
            job.StartDate = request.StartDate;
            job.EndDate = request.EndDate;

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
                return ServiceResult<JobResponse>.Forbidden("Only the job owner can publish this job.");
            }

            if (job.Status != JobStatus.Draft)
            {
                return ServiceResult<JobResponse>.Conflict("Only draft jobs can be published.");
            }

            var validation = await ValidateExistingJobAsync(job);
            if (validation is not null)
            {
                return validation;
            }

            var limitValidation = await ValidateSubscriptionLimitAsync(job.CompanyId);
            if (limitValidation is not null)
            {
                return limitValidation;
            }

            job.Status = JobStatus.Open;
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
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

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

            if (request.BudgetType == BudgetType.Fixed && request.FixedBudget <= 0)
            {
                return ServiceResult<JobResponse>.Validation(nameof(request.FixedBudget), "Fixed budget must be greater than zero.");
            }

            if (request.BudgetType == BudgetType.Hourly && request.HourlyRate <= 0)
            {
                return ServiceResult<JobResponse>.Validation(nameof(request.HourlyRate), "Hourly rate must be greater than zero.");
            }

            return null;
        }

        private async Task<ServiceResult<JobResponse>?> ValidateExistingJobAsync(Job job)
        {
            return await ValidateJobRequestAsync(new UpsertJobRequest
            {
                Title = job.Title,
                Description = job.Description,
                CategoryId = job.CategoryId,
                BudgetType = job.BudgetType,
                FixedBudget = job.FixedBudget,
                HourlyRate = job.HourlyRate,
                WorkersNeeded = job.WorkersNeeded,
                StartDate = job.StartDate,
                EndDate = job.EndDate
            });
        }

        private async Task<ServiceResult<JobResponse>?> ValidateSubscriptionLimitAsync(int companyId)
        {
            var activeSubscription = await _context.CompanySubscriptions
                .Include(subscription => subscription.Plan)
                .Where(subscription => subscription.CompanyId == companyId && subscription.IsActive)
                .OrderByDescending(subscription => subscription.StartDate)
                .FirstOrDefaultAsync();

            if (activeSubscription is null || activeSubscription.Plan.JobLimitPerMonth == int.MaxValue)
            {
                return null;
            }

            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var publishedThisMonth = await _context.Jobs.CountAsync(job =>
                job.CompanyId == companyId &&
                job.Status != JobStatus.Draft &&
                job.CreatedAt >= monthStart);

            if (publishedThisMonth >= activeSubscription.Plan.JobLimitPerMonth)
            {
                return ServiceResult<JobResponse>.Conflict(
                    $"The active {activeSubscription.Plan.Name} plan allows {activeSubscription.Plan.JobLimitPerMonth} published jobs per month.");
            }

            return null;
        }
    }
}
