using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuest.Authorization;
using SideQuest.Data;
using SideQuest.Models;
using SideQuest.ViewModels;

namespace SideQuest.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private const int XpPerLevel = 500;

        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            if (User.IsInRole(SideQuestRoles.Admin))
            {
                return RedirectToAction(nameof(Admin));
            }

            if (User.IsInRole(SideQuestRoles.Employer))
            {
                return RedirectToAction(nameof(Employer));
            }

            return RedirectToAction(nameof(Worker));
        }

        [Authorize(Roles = SideQuestRoles.Worker)]
        public async Task<IActionResult> Worker()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            ViewData["Title"] = "Worker Dashboard";
            ViewData["TopBarTitle"] = "Worker Portal";
            ViewData["Shell"] = "App";
            ViewData["ActivePortal"] = "Worker";
            ViewData["ActiveNav"] = "Dashboard";

            var user = await _context.Users
                .AsNoTracking()
                .Include(applicationUser => applicationUser.WorkerProfile)
                .Include(applicationUser => applicationUser.Wallet)
                .Include(applicationUser => applicationUser.UserXP)
                .FirstOrDefaultAsync(applicationUser => applicationUser.Id == userId);

            if (user is null)
            {
                return Challenge();
            }

            var activeAssignments = await _context.JobAssignments
                .AsNoTracking()
                .Include(assignment => assignment.Job)
                    .ThenInclude(job => job.Company)
                .Include(assignment => assignment.Worker)
                .Where(assignment => assignment.WorkerId == userId && !assignment.IsCompleted)
                .OrderByDescending(assignment => assignment.Job.CreatedAt)
                .Take(4)
                .ToListAsync();

            var recentApplications = await _context.JobApplications
                .AsNoTracking()
                .Include(application => application.Job)
                    .ThenInclude(job => job.Company)
                .Where(application => application.WorkerId == userId)
                .OrderByDescending(application => application.AppliedAt)
                .Take(5)
                .ToListAsync();

            var suggestedJobs = await _context.Jobs
                .AsNoTracking()
                .Include(job => job.Company)
                .Include(job => job.Category)
                .Include(job => job.Assignments)
                .Where(job => job.Status == JobStatus.Open)
                .OrderByDescending(job => job.CreatedAt)
                .Take(4)
                .ToListAsync();

            var notifications = await _context.Notifications
                .AsNoTracking()
                .Where(notification => notification.UserId == userId)
                .OrderByDescending(notification => notification.CreatedAt)
                .Take(4)
                .Select(notification => new DashboardNotificationViewModel
                {
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt
                })
                .ToListAsync();

            var achievementNames = await _context.UserAchievements
                .AsNoTracking()
                .Include(userAchievement => userAchievement.Achievement)
                .Where(userAchievement => userAchievement.UserId == userId)
                .OrderByDescending(userAchievement => userAchievement.EarnedAt)
                .Take(3)
                .Select(userAchievement => userAchievement.Achievement.Name)
                .ToListAsync();

            var pendingApplicationsCount = await _context.JobApplications
                .AsNoTracking()
                .CountAsync(application => application.WorkerId == userId && application.Status == ApplicationStatus.Pending);

            var xp = user.UserXP?.XP ?? 0;

            var model = new WorkerDashboardViewModel
            {
                DisplayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Worker" : user.FullName,
                Headline = string.IsNullOrWhiteSpace(user.WorkerProfile?.Headline) ? "Ready for the next quest" : user.WorkerProfile.Headline,
                Level = user.UserXP?.Level ?? 1,
                XP = xp,
                XPProgressPercent = Math.Clamp(xp % XpPerLevel * 100 / XpPerLevel, 0, 100),
                WalletBalance = user.Wallet?.CurrentBalance ?? 0,
                TotalEarned = user.Wallet?.TotalEarned ?? 0,
                ActiveAssignmentsCount = activeAssignments.Count,
                PendingApplicationsCount = pendingApplicationsCount,
                CompletedJobs = user.WorkerProfile?.TotalJobsCompleted ?? 0,
                AverageRating = user.WorkerProfile?.AverageRating ?? 0,
                ActiveAssignments = activeAssignments.Select(ToAssignmentViewModel).ToList(),
                RecentApplications = recentApplications.Select(ToApplicationViewModel).ToList(),
                SuggestedJobs = suggestedJobs.Select(ToJobViewModel).ToList(),
                Notifications = notifications,
                AchievementNames = achievementNames
            };

            return View(model);
        }

        [Authorize(Roles = SideQuestRoles.Employer)]
        public async Task<IActionResult> Employer()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            ViewData["Title"] = "Employer Dashboard";
            ViewData["TopBarTitle"] = "Company Portal";
            ViewData["Shell"] = "App";
            ViewData["ActivePortal"] = "Employer";
            ViewData["ActiveNav"] = "Dashboard";

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(applicationUser => applicationUser.Id == userId);

            var company = await _context.CompanyProfiles
                .AsNoTracking()
                .Include(companyProfile => companyProfile.CompanySubscriptions)
                    .ThenInclude(subscription => subscription.Plan)
                .FirstOrDefaultAsync(companyProfile => companyProfile.UserId == userId);

            if (company is null)
            {
                return View(new EmployerDashboardViewModel
                {
                    DisplayName = user?.FullName ?? user?.Email ?? "Company",
                    NeedsCompanyProfile = true
                });
            }

            var jobs = await _context.Jobs
                .AsNoTracking()
                .Include(job => job.Category)
                .Include(job => job.Company)
                .Include(job => job.Applications)
                .Include(job => job.Assignments)
                .Where(job => job.CompanyId == company.Id)
                .OrderByDescending(job => job.CreatedAt)
                .Take(6)
                .ToListAsync();

            var recentApplications = await _context.JobApplications
                .AsNoTracking()
                .Include(application => application.Job)
                    .ThenInclude(job => job.Company)
                .Where(application => application.Job.CompanyId == company.Id)
                .OrderByDescending(application => application.AppliedAt)
                .Take(5)
                .ToListAsync();

            var activeAssignments = await _context.JobAssignments
                .AsNoTracking()
                .Include(assignment => assignment.Job)
                    .ThenInclude(job => job.Company)
                .Include(assignment => assignment.Worker)
                .Where(assignment => assignment.Job.CompanyId == company.Id && !assignment.IsCompleted)
                .OrderByDescending(assignment => assignment.Job.CreatedAt)
                .Take(5)
                .ToListAsync();

            var activeJobsCount = await _context.Jobs
                .AsNoTracking()
                .CountAsync(job =>
                    job.CompanyId == company.Id &&
                    job.Status != JobStatus.Draft &&
                    job.Status != JobStatus.Completed &&
                    job.Status != JobStatus.Cancelled);

            var draftJobsCount = await _context.Jobs
                .AsNoTracking()
                .CountAsync(job => job.CompanyId == company.Id && job.Status == JobStatus.Draft);

            var totalApplicationsCount = await _context.JobApplications
                .AsNoTracking()
                .CountAsync(application => application.Job.CompanyId == company.Id);

            var commissionTotal = await _context.Commissions
                .AsNoTracking()
                .Where(commission => commission.CompanyId == company.Id)
                .SumAsync(commission => (decimal?)commission.Amount) ?? 0;

            var activeSubscription = company.CompanySubscriptions
                .Where(subscription => subscription.IsActive)
                .OrderByDescending(subscription => subscription.StartDate)
                .FirstOrDefault();

            var model = new EmployerDashboardViewModel
            {
                DisplayName = user?.FullName ?? user?.Email ?? "Company",
                CompanyName = company.CompanyName,
                SubscriptionName = activeSubscription?.Plan.Name ?? "No active plan",
                ActiveJobsCount = activeJobsCount,
                DraftJobsCount = draftJobsCount,
                TotalApplicationsCount = totalApplicationsCount,
                ActiveAssignmentsCount = activeAssignments.Count,
                CommissionTotal = commissionTotal,
                RecentJobs = jobs.Select(ToJobViewModel).ToList(),
                RecentApplications = recentApplications.Select(ToApplicationViewModel).ToList(),
                ActiveAssignments = activeAssignments.Select(ToAssignmentViewModel).ToList()
            };

            return View(model);
        }

        [Authorize(Roles = SideQuestRoles.Admin)]
        public async Task<IActionResult> Admin()
        {
            ViewData["Title"] = "Admin Dashboard";
            ViewData["TopBarTitle"] = "Command Center";
            ViewData["Shell"] = "App";
            ViewData["ActivePortal"] = "Admin";
            ViewData["ActiveNav"] = "Dashboard";

            var recentUsers = await _context.Users
                .AsNoTracking()
                .OrderByDescending(user => user.CreatedAt)
                .Take(8)
                .ToListAsync();

            var userRows = new List<AdminUserRowViewModel>();
            foreach (var user in recentUsers)
            {
                userRows.Add(new AdminUserRowViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "User" : user.FullName,
                    IsActive = user.IsActive,
                    Roles = (await _userManager.GetRolesAsync(user)).ToList()
                });
            }

            var recentJobs = await _context.Jobs
                .AsNoTracking()
                .Include(job => job.Company)
                .Include(job => job.Category)
                .Include(job => job.Assignments)
                .OrderByDescending(job => job.CreatedAt)
                .Take(5)
                .ToListAsync();

            var platformCommissionTotal = await _context.Commissions
                .AsNoTracking()
                .SumAsync(commission => (decimal?)commission.Amount) ?? 0;

            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.AsNoTracking().CountAsync(),
                TotalWorkers = await CountUsersInRoleAsync(SideQuestRoles.Worker),
                TotalEmployers = await CountUsersInRoleAsync(SideQuestRoles.Employer),
                TotalJobs = await _context.Jobs.AsNoTracking().CountAsync(),
                OpenJobs = await _context.Jobs.AsNoTracking().CountAsync(job => job.Status == JobStatus.Open),
                CategoryCount = await _context.Categories.AsNoTracking().CountAsync(),
                AchievementCount = await _context.Achievements.AsNoTracking().CountAsync(),
                PlatformCommissionTotal = platformCommissionTotal,
                RecentUsers = userRows,
                RecentJobs = recentJobs.Select(ToJobViewModel).ToList()
            };

            return View(model);
        }

        private async Task<int> CountUsersInRoleAsync(string role)
        {
            var users = await _userManager.GetUsersInRoleAsync(role);
            return users.Count;
        }

        private static DashboardJobViewModel ToJobViewModel(Job job)
        {
            return new DashboardJobViewModel
            {
                Id = job.Id,
                Title = job.Title,
                CompanyName = job.Company.CompanyName,
                CategoryName = job.Category.Name,
                RewardLabel = FormatReward(job),
                WorkersNeeded = job.WorkersNeeded,
                AcceptedWorkers = job.Assignments.Count,
                Status = job.Status,
                CreatedAt = job.CreatedAt
            };
        }

        private static DashboardApplicationViewModel ToApplicationViewModel(JobApplication application)
        {
            return new DashboardApplicationViewModel
            {
                Id = application.Id,
                JobTitle = application.Job.Title,
                CompanyName = application.Job.Company.CompanyName,
                Status = application.Status,
                AppliedAt = application.AppliedAt
            };
        }

        private static DashboardAssignmentViewModel ToAssignmentViewModel(JobAssignment assignment)
        {
            return new DashboardAssignmentViewModel
            {
                Id = assignment.Id,
                JobTitle = assignment.Job.Title,
                WorkerName = string.IsNullOrWhiteSpace(assignment.Worker.FullName) ? assignment.Worker.Email ?? "Worker" : assignment.Worker.FullName,
                CompanyName = assignment.Job.Company.CompanyName,
                AgreedRate = assignment.AgreedRate,
                Earnings = assignment.Earnings,
                IsCompleted = assignment.IsCompleted,
                CompletedAt = assignment.CompletedAt
            };
        }

        private static string FormatReward(Job job)
        {
            return job.BudgetType == BudgetType.Fixed
                ? job.FixedBudget.ToString("C0", CultureInfo.CurrentCulture)
                : $"{job.HourlyRate.ToString("C0", CultureInfo.CurrentCulture)}/hr";
        }
    }
}
