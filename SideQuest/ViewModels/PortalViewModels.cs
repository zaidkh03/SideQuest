using System.ComponentModel.DataAnnotations;
using SideQuest.Contracts;
using SideQuest.Models;

namespace SideQuest.ViewModels
{
    public sealed class CategoryOptionViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed class PortalJobViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public string RewardLabel { get; set; } = string.Empty;

        public BudgetType BudgetType { get; set; }

        public decimal FixedBudget { get; set; }

        public decimal HourlyRate { get; set; }

        public decimal HoursPerDay { get; set; }

        public int DurationDays { get; set; }

        public int WorkersNeeded { get; set; }

        public int AcceptedWorkers { get; set; }

        public JobStatus Status { get; set; }

        public decimal OfferedCommissionRate { get; set; }

        public decimal? RequiredCommissionRate { get; set; }

        public decimal? ApprovedCommissionRate { get; set; }

        public string? CommissionReviewNote { get; set; }

        public DateTime? CommissionReviewedAt { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public sealed class WorkerJobsPageViewModel
    {
        public string? Search { get; set; }

        public int? CategoryId { get; set; }

        public BudgetType? BudgetType { get; set; }

        public IReadOnlyList<CategoryOptionViewModel> Categories { get; set; } = [];

        public IReadOnlyList<PortalJobViewModel> Jobs { get; set; } = [];
    }

    public sealed class ApplyToJobFormViewModel
    {
        [Required]
        [MaxLength(4000)]
        [Display(Name = "Cover letter")]
        public string CoverLetter { get; set; } = string.Empty;
    }

    public sealed class WorkerJobDetailViewModel
    {
        public PortalJobViewModel Job { get; set; } = new();

        public bool CanApply { get; set; }

        public bool HasApplied { get; set; }

        public ApplicationStatus? ApplicationStatus { get; set; }

        public ApplyToJobFormViewModel ApplyForm { get; set; } = new();
    }

    public sealed class PortalApplicationViewModel
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public string WorkerId { get; set; } = string.Empty;

        public string WorkerName { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string CoverLetter { get; set; } = string.Empty;

        public ApplicationStatus Status { get; set; }

        public DateTime AppliedAt { get; set; }
    }

    public sealed class WorkerApplicationsPageViewModel
    {
        public IReadOnlyList<PortalApplicationViewModel> Applications { get; set; } = [];
    }

    public sealed class WorkerWorkPageViewModel
    {
        public IReadOnlyList<PortalApplicationViewModel> Applications { get; set; } = [];

        public IReadOnlyList<PortalAssignmentViewModel> Assignments { get; set; } = [];
    }

    public sealed class PortalAssignmentViewModel
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public string WorkerId { get; set; } = string.Empty;

        public string WorkerName { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public BudgetType BudgetType { get; set; }

        public decimal AgreedRate { get; set; }

        public decimal HoursWorked { get; set; }

        public decimal Earnings { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedAt { get; set; }
    }

    public sealed class WorkerAssignmentsPageViewModel
    {
        public IReadOnlyList<PortalAssignmentViewModel> Assignments { get; set; } = [];
    }

    public sealed class WalletPageViewModel
    {
        public decimal CurrentBalance { get; set; }

        public decimal TotalEarned { get; set; }

        public decimal TotalWithdrawn { get; set; }

        public WithdrawalFormViewModel Withdrawal { get; set; } = new();

        public IReadOnlyList<TransactionResponse> Transactions { get; set; } = [];
    }

    public sealed class WithdrawalFormViewModel
    {
        [Range(1, 1000000)]
        public decimal Amount { get; set; }
    }

    public sealed class CompanyProfilePageViewModel
    {
        public bool IsVerified { get; set; }

        public VerificationStatus VerificationStatus { get; set; }

        public CompanyProfileFormViewModel Form { get; set; } = new();
    }

    public sealed class CompanyProfileFormViewModel
    {
        [Required]
        [MaxLength(200)]
        [Display(Name = "Company name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Website { get; set; }

        [MaxLength(500)]
        [Display(Name = "Logo URL")]
        public string? LogoUrl { get; set; }
    }

    public sealed class EmployerJobsPageViewModel
    {
        public JobFormViewModel Form { get; set; } = new();

        public IReadOnlyList<CategoryOptionViewModel> Categories { get; set; } = [];

        public IReadOnlyList<PortalJobViewModel> Jobs { get; set; } = [];
    }

    public sealed class JobFormViewModel
    {
        public int? Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Budget type")]
        public BudgetType BudgetType { get; set; } = BudgetType.Hourly;

        [Range(0, 1000000)]
        [Display(Name = "Fixed budget")]
        public decimal FixedBudget { get; set; }

        [Range(0, 1000000)]
        [Display(Name = "Hourly rate")]
        public decimal HourlyRate { get; set; }

        [Range(0.25, 24)]
        [Display(Name = "Hours per day")]
        public decimal HoursPerDay { get; set; } = 8;

        [Range(1, 365)]
        [Display(Name = "Number of days")]
        public int DurationDays { get; set; } = 1;

        [Range(1, 1000)]
        [Display(Name = "Workers needed")]
        public int WorkersNeeded { get; set; } = 1;

        [DataType(DataType.DateTime)]
        [Display(Name = "Start date")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(1);

        [DataType(DataType.DateTime)]
        [Display(Name = "End date")]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(2);

        [Range(10, 100)]
        [Display(Name = "Platform commission %")]
        public decimal OfferedCommissionRate { get; set; } = 10;

        public IReadOnlyList<CategoryOptionViewModel> Categories { get; set; } = [];
    }

    public sealed class EmployerJobDetailViewModel
    {
        public PortalJobViewModel Job { get; set; } = new();

        public IReadOnlyList<PortalApplicationViewModel> Applications { get; set; } = [];

        public IReadOnlyList<PortalAssignmentViewModel> Assignments { get; set; } = [];

        public IReadOnlyList<ReviewResponse> Reviews { get; set; } = [];
    }

    public sealed class CompleteAssignmentFormViewModel
    {
        public int AssignmentId { get; set; }

        public int JobId { get; set; }

        [Range(0, 1000000)]
        [Display(Name = "Hours worked")]
        public decimal? HoursWorked { get; set; }
    }

    public sealed class ReviewWorkerFormViewModel
    {
        public int JobId { get; set; }

        [Required]
        public string ReviewedUserId { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [Required]
        [MaxLength(2000)]
        public string Comment { get; set; } = string.Empty;
    }

    public sealed class ProfileOverviewViewModel
    {
        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string Initials { get; set; } = string.Empty;

        public string? ProfileImageUrl { get; set; }

        public string RoleLabel { get; set; } = "Account";

        public string Headline { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public VerificationStatus? VerificationStatus { get; set; }

        public IReadOnlyList<StatCardViewModel> Stats { get; set; } = [];

        public IReadOnlyList<UserSkillResponse> Skills { get; set; } = [];

        public IReadOnlyList<string> Achievements { get; set; } = [];
    }

    public sealed class ProfileEditViewModel
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Profile image URL")]
        public string? ProfileImageUrl { get; set; }

        [Phone]
        [MaxLength(40)]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        public string RoleLabel { get; set; } = string.Empty;

        public WorkerProfileEditSectionViewModel Worker { get; set; } = new();

        public CompanyProfileFormViewModel Company { get; set; } = new();
    }

    public sealed class WorkerProfileEditSectionViewModel
    {
        public string Headline { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public decimal? HourlyRatePreference { get; set; }

        public AvailabilityStatus AvailabilityStatus { get; set; } = AvailabilityStatus.Available;

        public string? PortfolioUrl { get; set; }

        public string? ResumeUrl { get; set; }

        public int ExperienceYears { get; set; }
    }

    public sealed class WorkerSkillsPageViewModel
    {
        public IReadOnlyList<SkillSelectionViewModel> Skills { get; set; } = [];
    }

    public sealed class SkillSelectionViewModel
    {
        public int SkillId { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsSelected { get; set; }

        public int SkillLevel { get; set; } = 1;
    }

    public sealed class UpdateSkillsFormViewModel
    {
        public List<int> SelectedSkillIds { get; set; } = [];

        public Dictionary<int, int> SkillLevels { get; set; } = [];
    }

    public sealed class NotificationsPageViewModel
    {
        public bool UnreadOnly { get; set; }

        public IReadOnlyList<NotificationResponse> Notifications { get; set; } = [];
    }

    public sealed class AdminUsersPageViewModel
    {
        public IReadOnlyList<AdminUserResponse> Users { get; set; } = [];
    }

    public sealed class CategoryAdminPageViewModel
    {
        public CategoryFormViewModel Form { get; set; } = new();

        public IReadOnlyList<CategoryResponse> Categories { get; set; } = [];
    }

    public sealed class CategoryFormViewModel
    {
        public int? Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public sealed class AchievementAdminPageViewModel
    {
        public AchievementFormViewModel Form { get; set; } = new();

        public IReadOnlyList<AchievementResponse> Achievements { get; set; } = [];
    }

    public sealed class AchievementFormViewModel
    {
        public int? Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        [Display(Name = "XP required")]
        public int XPRequired { get; set; }

        [Required]
        [MaxLength(500)]
        [Display(Name = "Badge image URL")]
        public string BadgeImageUrl { get; set; } = string.Empty;
    }

    public sealed class AdminJobsPageViewModel
    {
        public string? Search { get; set; }

        public JobStatus? Status { get; set; }

        public BudgetType? BudgetType { get; set; }

        public IReadOnlyList<PortalJobViewModel> Jobs { get; set; } = [];
    }

    public sealed class AdminJobCommissionFormViewModel
    {
        [Range(10, 100)]
        [Display(Name = "Required commission %")]
        public decimal RequiredCommissionRate { get; set; } = 15;

        [Required]
        [MaxLength(1000)]
        public string Note { get; set; } = string.Empty;
    }

    public sealed class AdminFinancePageViewModel
    {
        public decimal PlatformCommissionTotal { get; set; }

        public decimal CompletedEarningsTotal { get; set; }

        public decimal PendingWithdrawalsTotal { get; set; }

        public IReadOnlyList<TransactionLedgerRowViewModel> Transactions { get; set; } = [];
    }

    public sealed class TransactionLedgerRowViewModel
    {
        public int Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string? JobTitle { get; set; }

        public decimal Amount { get; set; }

        public TransactionType Type { get; set; }

        public TransactionStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public sealed class VerificationListPageViewModel
    {
        public string QueueType { get; set; } = string.Empty;

        public string? Search { get; set; }

        public VerificationStatus? Status { get; set; }

        public IReadOnlyList<WorkerVerificationQueueItemViewModel> Workers { get; set; } = [];

        public IReadOnlyList<CompanyVerificationQueueItemViewModel> Companies { get; set; } = [];
    }

    public sealed class CommunityIndexViewModel
    {
        public CommunityPostFormViewModel Form { get; set; } = new();

        public IReadOnlyList<CommunityPostCardViewModel> Posts { get; set; } = [];
    }

    public sealed class CommunityPostCardViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ContentPreview { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        public string AuthorInitials { get; set; } = string.Empty;

        public CommunityPostType Type { get; set; }

        public int CommentCount { get; set; }

        public int LikeCount { get; set; }

        public bool IsLikedByCurrentUser { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public sealed class CommunityPostDetailViewModel
    {
        public CommunityPostCardViewModel Post { get; set; } = new();

        public string Content { get; set; } = string.Empty;

        public CommunityCommentFormViewModel CommentForm { get; set; } = new();

        public IReadOnlyList<CommunityCommentViewModel> Comments { get; set; } = [];
    }

    public sealed class CommunityPostFormViewModel
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public CommunityPostType Type { get; set; } = CommunityPostType.Discussion;
    }

    public sealed class CommunityCommentFormViewModel
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }

    public sealed class CommunityCommentViewModel
    {
        public string AuthorName { get; set; } = string.Empty;

        public string AuthorInitials { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
