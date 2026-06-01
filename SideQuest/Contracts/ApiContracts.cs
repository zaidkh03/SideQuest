using System.ComponentModel.DataAnnotations;
using SideQuest.Models;

namespace SideQuest.Contracts
{
    public sealed class CurrentUserResponse
    {
        public string? Id { get; set; }

        public string? Email { get; set; }

        public string? FullName { get; set; }

        public IReadOnlyList<string> Roles { get; set; } = [];

        public bool IsAuthenticated { get; set; }

        public bool HasWorkerProfile { get; set; }

        public bool HasCompanyProfile { get; set; }

        public bool HasWallet { get; set; }
    }

    public sealed class UpsertWorkerProfileRequest
    {
        [Required]
        [MaxLength(200)]
        public string Headline { get; set; } = string.Empty;

        [Required]
        public string Bio { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Range(0, 1000000)]
        public decimal? HourlyRatePreference { get; set; }

        public AvailabilityStatus AvailabilityStatus { get; set; } = AvailabilityStatus.Available;

        [MaxLength(500)]
        public string? PortfolioUrl { get; set; }

        [MaxLength(500)]
        public string? ResumeUrl { get; set; }

        [Range(0, 80)]
        public int ExperienceYears { get; set; }
    }

    public sealed class WorkerProfileResponse
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Headline { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public decimal? HourlyRatePreference { get; set; }

        public AvailabilityStatus AvailabilityStatus { get; set; }

        public string? PortfolioUrl { get; set; }

        public string? ResumeUrl { get; set; }

        public int ExperienceYears { get; set; }

        public int TotalJobsCompleted { get; set; }

        public decimal AverageRating { get; set; }

        public IReadOnlyList<UserSkillResponse> Skills { get; set; } = [];
    }

    public sealed class UpdateWorkerSkillsRequest
    {
        [Required]
        public List<UserSkillRequest> Skills { get; set; } = [];
    }

    public sealed class UserSkillRequest
    {
        [Range(1, int.MaxValue)]
        public int SkillId { get; set; }

        [Range(1, 5)]
        public int SkillLevel { get; set; } = 1;
    }

    public sealed class UserSkillResponse
    {
        public int SkillId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int SkillLevel { get; set; }
    }

    public sealed class UpsertCompanyProfileRequest
    {
        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Website { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }
    }

    public sealed class CompanyProfileResponse
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string? Website { get; set; }

        public string? LogoUrl { get; set; }

        public bool IsVerified { get; set; }

        public SubscriptionSummaryResponse? ActiveSubscription { get; set; }
    }

    public sealed class SubscriptionSummaryResponse
    {
        public string PlanName { get; set; } = string.Empty;

        public int JobLimitPerMonth { get; set; }

        public decimal CommissionRate { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }

    public sealed class JobQueryParameters
    {
        public string? Search { get; set; }

        public int? CategoryId { get; set; }

        public JobStatus? Status { get; set; }

        public BudgetType? BudgetType { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }

    public sealed class UpsertJobRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }

        public BudgetType BudgetType { get; set; }

        [Range(0, 1000000)]
        public decimal FixedBudget { get; set; }

        [Range(0, 1000000)]
        public decimal HourlyRate { get; set; }

        [Range(1, 1000)]
        public int WorkersNeeded { get; set; } = 1;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }

    public sealed class JobResponse
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public BudgetType BudgetType { get; set; }

        public decimal FixedBudget { get; set; }

        public decimal HourlyRate { get; set; }

        public int WorkersNeeded { get; set; }

        public int AcceptedWorkers { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public JobStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public sealed class CreateApplicationRequest
    {
        [Required]
        [MaxLength(4000)]
        public string CoverLetter { get; set; } = string.Empty;
    }

    public sealed class JobApplicationResponse
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public string WorkerId { get; set; } = string.Empty;

        public string WorkerName { get; set; } = string.Empty;

        public string CoverLetter { get; set; } = string.Empty;

        public ApplicationStatus Status { get; set; }

        public DateTime AppliedAt { get; set; }
    }

    public sealed class CompleteAssignmentRequest
    {
        [Range(0, 1000000)]
        public decimal? HoursWorked { get; set; }
    }

    public sealed class JobAssignmentResponse
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public string WorkerId { get; set; } = string.Empty;

        public string WorkerName { get; set; } = string.Empty;

        public decimal AgreedRate { get; set; }

        public decimal HoursWorked { get; set; }

        public decimal Earnings { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedAt { get; set; }
    }

    public sealed class CreateReviewRequest
    {
        [Range(1, int.MaxValue)]
        public int JobId { get; set; }

        [Required]
        public string ReviewedUserId { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Comment { get; set; } = string.Empty;
    }

    public sealed class ReviewResponse
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        public string ReviewerId { get; set; } = string.Empty;

        public string ReviewerName { get; set; } = string.Empty;

        public string ReviewedUserId { get; set; } = string.Empty;

        public string ReviewedUserName { get; set; } = string.Empty;

        public int Rating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }

    public sealed class NotificationResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public sealed class WalletResponse
    {
        public decimal CurrentBalance { get; set; }

        public decimal TotalEarned { get; set; }

        public decimal TotalWithdrawn { get; set; }

        public IReadOnlyList<TransactionResponse> RecentTransactions { get; set; } = [];
    }

    public sealed class CreateWithdrawalRequest
    {
        [Range(1, 1000000)]
        public decimal Amount { get; set; }
    }

    public sealed class TransactionResponse
    {
        public int Id { get; set; }

        public int? JobId { get; set; }

        public decimal Amount { get; set; }

        public TransactionType Type { get; set; }

        public TransactionStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public sealed class CategoryRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public sealed class CategoryResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public sealed class AchievementRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int XPRequired { get; set; }

        [Required]
        [MaxLength(500)]
        public string BadgeImageUrl { get; set; } = string.Empty;
    }

    public sealed class AchievementResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int XPRequired { get; set; }

        public string BadgeImageUrl { get; set; } = string.Empty;
    }

    public sealed class AdminUserResponse
    {
        public string Id { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string FullName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public IReadOnlyList<string> Roles { get; set; } = [];
    }

    public sealed class UpdateUserStatusRequest
    {
        public bool IsActive { get; set; }
    }
}
