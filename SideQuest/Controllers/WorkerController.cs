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
    [Authorize(Roles = SideQuestRoles.Worker)]
    public class WorkerController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly IAssignmentService _assignmentService;
        private readonly AppDbContext _context;
        private readonly IJobService _jobService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWalletService _walletService;

        public WorkerController(
            AppDbContext context,
            IJobService jobService,
            IApplicationService applicationService,
            IAssignmentService assignmentService,
            IWalletService walletService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _jobService = jobService;
            _applicationService = applicationService;
            _assignmentService = assignmentService;
            _walletService = walletService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Jobs(string? search, int? categoryId, BudgetType? budgetType)
        {
            SetWorkerViewData("Jobs", "Quest Board");

            var result = await _jobService.GetOpenJobsAsync(new JobQueryParameters
            {
                Search = search,
                CategoryId = categoryId,
                BudgetType = budgetType,
                PageSize = 100
            });

            var model = new WorkerJobsPageViewModel
            {
                Search = search,
                CategoryId = categoryId,
                BudgetType = budgetType,
                Categories = await GetCategoryOptionsAsync(),
                Jobs = result.Value?.Select(job => job.ToPortalJob()).ToList() ?? []
            };

            return View(model);
        }

        public async Task<IActionResult> Job(int id)
        {
            SetWorkerViewData("Jobs", "Quest Details");

            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var jobResult = await _jobService.GetJobAsync(id, userId, isAdmin: false);
            if (!jobResult.Succeeded || jobResult.Value is null)
            {
                return NotFound();
            }

            var application = await _context.JobApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(existingApplication => existingApplication.JobId == id && existingApplication.WorkerId == userId);

            var model = new WorkerJobDetailViewModel
            {
                Job = jobResult.Value.ToPortalJob(),
                HasApplied = application is not null,
                ApplicationStatus = application?.Status,
                CanApply = jobResult.Value.Status == JobStatus.Open && application is null
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int id, ApplyToJobFormViewModel form)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Add a short cover letter before applying.";
                return RedirectToAction(nameof(Job), new { id });
            }

            var result = await _applicationService.ApplyAsync(userId, id, new CreateApplicationRequest
            {
                CoverLetter = form.CoverLetter
            });

            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "Application submitted."
                : result.Message ?? "We could not submit this application.";

            return RedirectToAction(nameof(Job), new { id });
        }

        public async Task<IActionResult> Applications()
        {
            SetWorkerViewData("Applications", "Applications");

            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var applications = await _context.JobApplications
                .AsNoTracking()
                .Include(application => application.Job)
                    .ThenInclude(job => job.Company)
                .Include(application => application.Worker)
                .Where(application => application.WorkerId == userId)
                .OrderByDescending(application => application.AppliedAt)
                .ToListAsync();

            return View(new WorkerApplicationsPageViewModel
            {
                Applications = applications.Select(application => application.ToPortalApplication()).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int id)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var result = await _applicationService.WithdrawAsync(userId, id);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "Application withdrawn."
                : result.Message ?? "We could not withdraw this application.";

            return RedirectToAction(nameof(Applications));
        }

        public async Task<IActionResult> Assignments()
        {
            SetWorkerViewData("Assignments", "Quest Log");

            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var assignments = await _context.JobAssignments
                .AsNoTracking()
                .Include(assignment => assignment.Job)
                    .ThenInclude(job => job.Company)
                .Include(assignment => assignment.Worker)
                .Where(assignment => assignment.WorkerId == userId)
                .OrderByDescending(assignment => assignment.Job.CreatedAt)
                .ToListAsync();

            return View(new WorkerAssignmentsPageViewModel
            {
                Assignments = assignments.Select(assignment => assignment.ToPortalAssignment()).ToList()
            });
        }

        public async Task<IActionResult> Wallet()
        {
            SetWorkerViewData("Wallet", "Wallet");

            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var result = await _walletService.GetWalletAsync(userId);
            if (!result.Succeeded || result.Value is null)
            {
                return View(new WalletPageViewModel());
            }

            return View(new WalletPageViewModel
            {
                CurrentBalance = result.Value.CurrentBalance,
                TotalEarned = result.Value.TotalEarned,
                TotalWithdrawn = result.Value.TotalWithdrawn,
                Transactions = result.Value.RecentTransactions
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestWithdrawal(WithdrawalFormViewModel form)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Enter a withdrawal amount greater than zero.";
                return RedirectToAction(nameof(Wallet));
            }

            var result = await _walletService.RequestWithdrawalAsync(userId, new CreateWithdrawalRequest
            {
                Amount = form.Amount
            });

            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "Withdrawal requested."
                : result.Message ?? "We could not request this withdrawal.";

            return RedirectToAction(nameof(Wallet));
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

        private string? GetUserId()
        {
            return _userManager.GetUserId(User);
        }

        private void SetWorkerViewData(string activeNav, string title)
        {
            ViewData["Title"] = title;
            ViewData["TopBarTitle"] = "Worker Portal";
            ViewData["Shell"] = "App";
            ViewData["ActivePortal"] = "Worker";
            ViewData["ActiveNav"] = activeNav;
        }
    }
}
