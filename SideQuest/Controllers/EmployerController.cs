using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuest.Authorization;
using SideQuest.Contracts;
using SideQuest.Data;
using SideQuest.Models;
using SideQuest.Services;
using SideQuest.ViewModels;

namespace SideQuest.Controllers
{
    [Authorize(Roles = SideQuestRoles.Employer)]
    public class EmployerController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly IAssignmentService _assignmentService;
        private readonly AppDbContext _context;
        private readonly IJobService _jobService;
        private readonly IProfileService _profileService;
        private readonly IReviewService _reviewService;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployerController(
            AppDbContext context,
            IJobService jobService,
            IApplicationService applicationService,
            IAssignmentService assignmentService,
            IProfileService profileService,
            IReviewService reviewService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _jobService = jobService;
            _applicationService = applicationService;
            _assignmentService = assignmentService;
            _profileService = profileService;
            _reviewService = reviewService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Company()
        {
            SetEmployerViewData("Company", "Company Profile");

            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var result = await _profileService.GetCompanyProfileAsync(userId);
            if (!result.Succeeded || result.Value is null)
            {
                return View(new CompanyProfilePageViewModel());
            }

            return View(new CompanyProfilePageViewModel
            {
                IsVerified = result.Value.IsVerified,
                VerificationStatus = result.Value.VerificationStatus,
                ActiveSubscription = result.Value.ActiveSubscription,
                Form = new CompanyProfileFormViewModel
                {
                    CompanyName = result.Value.CompanyName,
                    Description = result.Value.Description,
                    Location = result.Value.Location,
                    Website = result.Value.Website,
                    LogoUrl = result.Value.LogoUrl
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Company(CompanyProfileFormViewModel form)
        {
            SetEmployerViewData("Company", "Company Profile");

            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return View(new CompanyProfilePageViewModel { Form = form });
            }

            var result = await _profileService.UpsertCompanyProfileAsync(userId, new UpsertCompanyProfileRequest
            {
                CompanyName = form.CompanyName,
                Description = form.Description,
                Location = form.Location,
                Website = form.Website,
                LogoUrl = form.LogoUrl
            });

            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "Company profile updated."
                : result.Message ?? "We could not update the company profile.";

            return RedirectToAction(nameof(Company));
        }

        public async Task<IActionResult> Jobs()
        {
            SetEmployerViewData("Jobs", "Company Jobs");

            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var result = await _jobService.GetEmployerJobsAsync(userId);
            return View(new EmployerJobsPageViewModel
            {
                Jobs = result.Value?.Select(job => job.ToPortalJob()).ToList() ?? []
            });
        }

        public async Task<IActionResult> CreateJob()
        {
            SetEmployerViewData("PostQuest", "Post Quest");

            return View("JobForm", new JobFormViewModel
            {
                Categories = await GetCategoryOptionsAsync(),
                FixedBudget = 100,
                HourlyRate = 10
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJob(JobFormViewModel form)
        {
            SetEmployerViewData("PostQuest", "Post Quest");

            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                form.Categories = await GetCategoryOptionsAsync();
                return View("JobForm", form);
            }

            var result = await _jobService.CreateJobAsync(userId, ToJobRequest(form));
            if (!result.Succeeded || result.Value is null)
            {
                AddServiceErrors(result);
                form.Categories = await GetCategoryOptionsAsync();
                return View("JobForm", form);
            }

            TempData["SuccessMessage"] = "Job draft created.";
            return RedirectToAction(nameof(Details), new { id = result.Value.Id });
        }

        public async Task<IActionResult> EditJob(int id)
        {
            SetEmployerViewData("Jobs", "Edit Quest");

            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var result = await _jobService.GetJobAsync(id, userId, isAdmin: false);
            if (!result.Succeeded || result.Value is null)
            {
                return NotFound();
            }

            return View("JobForm", new JobFormViewModel
            {
                Id = result.Value.Id,
                Title = result.Value.Title,
                Description = result.Value.Description,
                CategoryId = result.Value.CategoryId,
                BudgetType = result.Value.BudgetType,
                FixedBudget = result.Value.FixedBudget,
                HourlyRate = result.Value.HourlyRate,
                WorkersNeeded = result.Value.WorkersNeeded,
                StartDate = result.Value.StartDate,
                EndDate = result.Value.EndDate,
                Categories = await GetCategoryOptionsAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(int id, JobFormViewModel form)
        {
            SetEmployerViewData("Jobs", "Edit Quest");

            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                form.Id = id;
                form.Categories = await GetCategoryOptionsAsync();
                return View("JobForm", form);
            }

            var result = await _jobService.UpdateJobAsync(userId, id, ToJobRequest(form));
            if (!result.Succeeded)
            {
                AddServiceErrors(result);
                form.Id = id;
                form.Categories = await GetCategoryOptionsAsync();
                return View("JobForm", form);
            }

            TempData["SuccessMessage"] = "Job updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Details(int id)
        {
            SetEmployerViewData("Jobs", "Quest Details");

            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var job = await _context.Jobs
                .AsNoTracking()
                .Include(existingJob => existingJob.Company)
                .Include(existingJob => existingJob.Category)
                .Include(existingJob => existingJob.Applications)
                    .ThenInclude(application => application.Worker)
                .Include(existingJob => existingJob.Assignments)
                    .ThenInclude(assignment => assignment.Worker)
                .Include(existingJob => existingJob.Reviews)
                    .ThenInclude(review => review.Reviewer)
                .Include(existingJob => existingJob.Reviews)
                    .ThenInclude(review => review.ReviewedUser)
                .FirstOrDefaultAsync(existingJob => existingJob.Id == id);

            if (job is null || job.Company.UserId != userId)
            {
                return NotFound();
            }

            return View(new EmployerJobDetailViewModel
            {
                Job = new JobResponse
                {
                    Id = job.Id,
                    CompanyId = job.CompanyId,
                    CompanyName = job.Company.CompanyName,
                    CategoryId = job.CategoryId,
                    CategoryName = job.Category.Name,
                    Title = job.Title,
                    Description = job.Description,
                    BudgetType = job.BudgetType,
                    FixedBudget = job.FixedBudget,
                    HourlyRate = job.HourlyRate,
                    WorkersNeeded = job.WorkersNeeded,
                    AcceptedWorkers = job.Assignments.Count,
                    StartDate = job.StartDate,
                    EndDate = job.EndDate,
                    Status = job.Status,
                    CreatedAt = job.CreatedAt
                }.ToPortalJob(),
                Applications = job.Applications.OrderByDescending(application => application.AppliedAt).Select(application => application.ToPortalApplication()).ToList(),
                Assignments = job.Assignments.OrderByDescending(assignment => assignment.CompletedAt ?? assignment.Job.CreatedAt).Select(assignment => assignment.ToPortalAssignment()).ToList(),
                Reviews = job.Reviews.OrderByDescending(review => review.CreatedAt).Select(review => review.ToResponse()).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var result = await _jobService.PublishJobAsync(userId, id);
            SetDecisionMessage(result, "Job published.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var result = await _jobService.CancelJobAsync(userId, id);
            SetDecisionMessage(result, "Job cancelled.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var result = await _jobService.CloseJobAsync(userId, id);
            SetDecisionMessage(result, "Job closed.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptApplication(int id)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var jobId = await GetApplicationJobIdAsync(id);
            var result = await _applicationService.AcceptAsync(userId, id);
            SetDecisionMessage(result, "Application accepted.");
            return RedirectToAction(nameof(Details), new { id = jobId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(int id)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var jobId = await GetApplicationJobIdAsync(id);
            var result = await _applicationService.RejectAsync(userId, id);
            SetDecisionMessage(result, "Application rejected.");
            return RedirectToAction(nameof(Details), new { id = jobId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAssignment(CompleteAssignmentFormViewModel form)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var result = await _assignmentService.CompleteAsync(userId, form.AssignmentId, new CompleteAssignmentRequest
            {
                HoursWorked = form.HoursWorked
            });
            SetDecisionMessage(result, "Assignment completed.");
            return RedirectToAction(nameof(Details), new { id = form.JobId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewWorker(ReviewWorkerFormViewModel form)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Choose a rating and write a short review.";
                return RedirectToAction(nameof(Details), new { id = form.JobId });
            }

            var result = await _reviewService.CreateReviewAsync(userId, new CreateReviewRequest
            {
                JobId = form.JobId,
                ReviewedUserId = form.ReviewedUserId,
                Rating = form.Rating,
                Comment = form.Comment
            });
            SetDecisionMessage(result, "Review submitted.");
            return RedirectToAction(nameof(Details), new { id = form.JobId });
        }

        private async Task<int> GetApplicationJobIdAsync(int applicationId)
        {
            return await _context.JobApplications
                .Where(application => application.Id == applicationId)
                .Select(application => application.JobId)
                .FirstOrDefaultAsync();
        }

        private async Task<IReadOnlyList<CategoryOptionViewModel>> GetCategoryOptionsAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(category => category.IsActive)
                .OrderBy(category => category.Name)
                .Select(category => new CategoryOptionViewModel
                {
                    Id = category.Id,
                    Name = category.Name
                })
                .ToListAsync();
        }

        private static UpsertJobRequest ToJobRequest(JobFormViewModel form)
        {
            return new UpsertJobRequest
            {
                Title = form.Title,
                Description = form.Description,
                CategoryId = form.CategoryId,
                BudgetType = form.BudgetType,
                FixedBudget = form.FixedBudget,
                HourlyRate = form.HourlyRate,
                WorkersNeeded = form.WorkersNeeded,
                StartDate = form.StartDate,
                EndDate = form.EndDate
            };
        }

        private void AddServiceErrors<T>(ServiceResult<T> result)
        {
            if (result.Errors.Count == 0)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "We could not save this job.");
                return;
            }

            foreach (var error in result.Errors)
            {
                foreach (var message in error.Value)
                {
                    ModelState.AddModelError(error.Key, message);
                }
            }
        }

        private void SetDecisionMessage<T>(ServiceResult<T> result, string successMessage)
        {
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? successMessage
                : result.Message ?? "We could not complete that action.";
        }

        private string? GetUserId()
        {
            return _userManager.GetUserId(User);
        }

        private void SetEmployerViewData(string activeNav, string title)
        {
            ViewData["Title"] = title;
            ViewData["TopBarTitle"] = "Company Portal";
            ViewData["Shell"] = "App";
            ViewData["ActivePortal"] = "Employer";
            ViewData["ActiveNav"] = activeNav;
        }
    }
}
