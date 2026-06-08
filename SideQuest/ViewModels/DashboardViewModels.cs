using SideQuest.Models;

namespace SideQuest.ViewModels
{
    public sealed class PageHeaderViewModel
    {
        public string? Eyebrow { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Subtitle { get; set; }

        public string? ActionText { get; set; }

        public string? ActionUrl { get; set; }

        public string? ActionIcon { get; set; }
    }

    public sealed class StatCardViewModel
    {
        public string Label { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public string? Detail { get; set; }

        public string Icon { get; set; } = "dashboard";

        public string Accent { get; set; } = "primary";

        public string? Trend { get; set; }
    }

    public sealed class DashboardJobViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public string RewardLabel { get; set; } = string.Empty;

        public decimal OfferedCommissionRate { get; set; }

        public decimal? RequiredCommissionRate { get; set; }

        public decimal? ApprovedCommissionRate { get; set; }

        public int WorkersNeeded { get; set; }

        public int AcceptedWorkers { get; set; }

        public JobStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public sealed class DashboardApplicationViewModel
    {
        public int Id { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public ApplicationStatus Status { get; set; }

        public DateTime AppliedAt { get; set; }
    }

    public sealed class DashboardAssignmentViewModel
    {
        public int Id { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public string WorkerName { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public decimal AgreedRate { get; set; }

        public decimal Earnings { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedAt { get; set; }
    }

    public sealed class DashboardNotificationViewModel
    {
        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public sealed class WorkerDashboardViewModel
    {
        public string DisplayName { get; set; } = "Worker";

        public string Headline { get; set; } = "Ready for the next quest";

        public int Level { get; set; } = 1;

        public int XP { get; set; }

        public int XPProgressPercent { get; set; }

        public decimal WalletBalance { get; set; }

        public decimal TotalEarned { get; set; }

        public int ActiveAssignmentsCount { get; set; }

        public int PendingApplicationsCount { get; set; }

        public int CompletedJobs { get; set; }

        public decimal AverageRating { get; set; }

        public IReadOnlyList<DashboardAssignmentViewModel> ActiveAssignments { get; set; } = [];

        public IReadOnlyList<DashboardApplicationViewModel> RecentApplications { get; set; } = [];

        public IReadOnlyList<DashboardJobViewModel> SuggestedJobs { get; set; } = [];

        public IReadOnlyList<DashboardNotificationViewModel> Notifications { get; set; } = [];

        public IReadOnlyList<string> AchievementNames { get; set; } = [];
    }

    public sealed class EmployerDashboardViewModel
    {
        public string DisplayName { get; set; } = "Company";

        public bool NeedsCompanyProfile { get; set; }

        public string CompanyName { get; set; } = "Company HQ";

        public int ActiveJobsCount { get; set; }

        public int DraftJobsCount { get; set; }

        public int TotalApplicationsCount { get; set; }

        public int ActiveAssignmentsCount { get; set; }

        public decimal CommissionTotal { get; set; }

        public IReadOnlyList<DashboardJobViewModel> RecentJobs { get; set; } = [];

        public IReadOnlyList<DashboardApplicationViewModel> RecentApplications { get; set; } = [];

        public IReadOnlyList<DashboardAssignmentViewModel> ActiveAssignments { get; set; } = [];
    }

    public sealed class AdminUserRowViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public IReadOnlyList<string> Roles { get; set; } = [];
    }

    public sealed class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }

        public int TotalWorkers { get; set; }

        public int TotalEmployers { get; set; }

        public int TotalJobs { get; set; }

        public int OpenJobs { get; set; }

        public int CategoryCount { get; set; }

        public int AchievementCount { get; set; }

        public decimal PlatformCommissionTotal { get; set; }

        public IReadOnlyList<AdminUserRowViewModel> RecentUsers { get; set; } = [];

        public IReadOnlyList<DashboardJobViewModel> RecentJobs { get; set; } = [];
    }
}
