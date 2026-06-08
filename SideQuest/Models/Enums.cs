namespace SideQuest.Models
{
    public enum AvailabilityStatus
    {
        Available,
        Busy,
        Unavailable
    }

    public enum VerificationStatus
    {
        Draft,
        Submitted,
        Approved,
        Rejected
    }

    public enum BudgetType
    {
        Fixed = 0,
        Hourly = 1
    }

    public enum JobStatus
    {
        Draft = 0,
        Open = 1,
        InProgress = 2,
        WaitingForReview = 3,
        Completed = 4,
        Overdue = 5,
        Cancelled = 6,
        PendingApproval = 7,
        NeedsCommissionUpdate = 8
    }

    public enum ApplicationStatus
    {
        Pending,
        Accepted,
        Rejected,
        Withdrawn
    }

    public enum TransactionType
    {
        Earning,
        Commission,
        Withdrawal,
        Refund
    }

    public enum TransactionStatus
    {
        Pending,
        Completed,
        Failed,
        Cancelled
    }

    public enum CommunityPostType
    {
        Volunteer,
        Event,
        Discussion,
        Announcement
    }
}
