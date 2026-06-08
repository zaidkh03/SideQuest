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
    [Authorize(Roles = SideQuestRoles.Admin)]
    [Route("Admin")]
    public class AdminManagementController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly AppDbContext _context;
        private readonly IJobService _jobService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminManagementController(
            AppDbContext context,
            IAdminService adminService,
            IJobService jobService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _adminService = adminService;
            _jobService = jobService;
            _userManager = userManager;
        }

        [HttpGet("Users")]
        public async Task<IActionResult> Users()
        {
            SetAdminViewData("Users", "Users");

            var result = await _adminService.GetUsersAsync();
            return View(new AdminUsersPageViewModel
            {
                Users = result.Value ?? []
            });
        }

        [HttpPost("Users/{userId}/Status")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserStatus(string userId, bool isActive)
        {
            var currentAdminId = _userManager.GetUserId(User);
            if (string.Equals(currentAdminId, userId, StringComparison.Ordinal))
            {
                TempData["ErrorMessage"] = "You cannot disable your own admin account.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _adminService.UpdateUserStatusAsync(userId, new UpdateUserStatusRequest { IsActive = isActive });
            SetDecisionMessage(result, isActive ? "User enabled." : "User disabled.");
            return RedirectToAction(nameof(Users));
        }

        [HttpGet("Categories")]
        public async Task<IActionResult> Categories(int? editId)
        {
            SetAdminViewData("Categories", "Categories");

            var result = await _adminService.GetCategoriesAsync();
            var categories = result.Value ?? [];
            var edit = editId.HasValue ? categories.FirstOrDefault(category => category.Id == editId.Value) : null;

            return View(new CategoryAdminPageViewModel
            {
                Categories = categories,
                Form = edit is null
                    ? new CategoryFormViewModel()
                    : new CategoryFormViewModel
                    {
                        Id = edit.Id,
                        Name = edit.Name,
                        Description = edit.Description,
                        IsActive = edit.IsActive
                    }
            });
        }

        [HttpPost("Categories")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCategory([Bind(Prefix = "Form")] CategoryFormViewModel form)
        {
            SetAdminViewData("Categories", "Categories");

            if (!ModelState.IsValid)
            {
                ViewData["OpenCategoryModal"] = true;
                var categories = await _adminService.GetCategoriesAsync();
                return View("Categories", new CategoryAdminPageViewModel
                {
                    Form = form,
                    Categories = categories.Value ?? []
                });
            }

            var request = new CategoryRequest
            {
                Name = form.Name,
                Description = form.Description,
                IsActive = form.IsActive
            };

            var result = form.Id.HasValue
                ? await _adminService.UpdateCategoryAsync(form.Id.Value, request)
                : await _adminService.CreateCategoryAsync(request);

            SetDecisionMessage(result, form.Id.HasValue ? "Category updated." : "Category created.");
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet("Achievements")]
        public async Task<IActionResult> Achievements(int? editId)
        {
            SetAdminViewData("Achievements", "Achievements");

            var result = await _adminService.GetAchievementsAsync();
            var achievements = result.Value ?? [];
            var edit = editId.HasValue ? achievements.FirstOrDefault(achievement => achievement.Id == editId.Value) : null;

            return View(new AchievementAdminPageViewModel
            {
                Achievements = achievements,
                Form = edit is null
                    ? new AchievementFormViewModel()
                    : new AchievementFormViewModel
                    {
                        Id = edit.Id,
                        Name = edit.Name,
                        Description = edit.Description,
                        XPRequired = edit.XPRequired,
                        BadgeImageUrl = edit.BadgeImageUrl
                    }
            });
        }

        [HttpPost("Achievements")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAchievement([Bind(Prefix = "Form")] AchievementFormViewModel form)
        {
            SetAdminViewData("Achievements", "Achievements");

            if (!ModelState.IsValid)
            {
                ViewData["OpenAchievementModal"] = true;
                var achievements = await _adminService.GetAchievementsAsync();
                return View("Achievements", new AchievementAdminPageViewModel
                {
                    Form = form,
                    Achievements = achievements.Value ?? []
                });
            }

            var request = new AchievementRequest
            {
                Name = form.Name,
                Description = form.Description,
                XPRequired = form.XPRequired,
                BadgeImageUrl = form.BadgeImageUrl
            };

            var result = form.Id.HasValue
                ? await _adminService.UpdateAchievementAsync(form.Id.Value, request)
                : await _adminService.CreateAchievementAsync(request);

            SetDecisionMessage(result, form.Id.HasValue ? "Achievement updated." : "Achievement created.");
            return RedirectToAction(nameof(Achievements));
        }

        [HttpGet("Jobs")]
        public async Task<IActionResult> Jobs(string? search, JobStatus? status, BudgetType? budgetType)
        {
            SetAdminViewData("Jobs", "Jobs");

            var result = await _jobService.GetAdminJobsAsync(new JobQueryParameters
            {
                Search = search,
                Status = status,
                BudgetType = budgetType,
                PageSize = 200
            });

            return View(new AdminJobsPageViewModel
            {
                Search = search,
                Status = status,
                BudgetType = budgetType,
                Jobs = result.Value?.Select(job => job.ToPortalJob()).ToList() ?? []
            });
        }

        [HttpPost("Jobs/{id:int}/Approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveJob(int id)
        {
            var adminUserId = _userManager.GetUserId(User);
            if (adminUserId is null)
            {
                return Challenge();
            }

            var result = await _jobService.ApproveJobCommissionAsync(adminUserId, id);
            SetDecisionMessage(result, "Job approved and opened to workers.");
            return RedirectToAction(nameof(Jobs));
        }

        [HttpPost("Jobs/{id:int}/RequestCommission")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCommissionUpdate(int id, AdminJobCommissionFormViewModel form)
        {
            var adminUserId = _userManager.GetUserId(User);
            if (adminUserId is null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Enter a required commission percentage and a short note.";
                return RedirectToAction(nameof(Jobs));
            }

            var result = await _jobService.RequestCommissionUpdateAsync(adminUserId, id, new JobCommissionUpdateRequest
            {
                RequiredCommissionRate = form.RequiredCommissionRate,
                Note = form.Note
            });
            SetDecisionMessage(result, "Commission update requested.");
            return RedirectToAction(nameof(Jobs));
        }

        [HttpGet("Finance")]
        public async Task<IActionResult> Finance()
        {
            SetAdminViewData("Finance", "Finance");

            var transactions = await _context.Transactions
                .AsNoTracking()
                .Include(transaction => transaction.User)
                .Include(transaction => transaction.Job)
                .OrderByDescending(transaction => transaction.CreatedAt)
                .Take(150)
                .ToListAsync();

            var model = new AdminFinancePageViewModel
            {
                PlatformCommissionTotal = await _context.Commissions.SumAsync(commission => (decimal?)commission.Amount) ?? 0,
                CompletedEarningsTotal = await _context.Transactions
                    .Where(transaction => transaction.Type == TransactionType.Earning && transaction.Status == TransactionStatus.Completed)
                    .SumAsync(transaction => (decimal?)transaction.Amount) ?? 0,
                PendingWithdrawalsTotal = await _context.Transactions
                    .Where(transaction => transaction.Type == TransactionType.Withdrawal && transaction.Status == TransactionStatus.Pending)
                    .SumAsync(transaction => (decimal?)transaction.Amount) ?? 0,
                Transactions = transactions.Select(transaction => new TransactionLedgerRowViewModel
                {
                    Id = transaction.Id,
                    UserName = PortalPageMapping.DisplayName(transaction.User),
                    JobTitle = transaction.Job?.Title,
                    Amount = transaction.Amount,
                    Type = transaction.Type,
                    Status = transaction.Status,
                    CreatedAt = transaction.CreatedAt
                }).ToList()
            };

            return View(model);
        }

        private void SetDecisionMessage<T>(ServiceResult<T> result, string successMessage)
        {
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? successMessage
                : result.Message ?? "We could not complete that action.";
        }

        private void SetAdminViewData(string activeNav, string title)
        {
            ViewData["Title"] = title;
            ViewData["TopBarTitle"] = "Command Center";
            ViewData["Shell"] = "App";
            ViewData["ActivePortal"] = "Admin";
            ViewData["ActiveNav"] = activeNav;
        }
    }
}
