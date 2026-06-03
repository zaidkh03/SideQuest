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
        Fixed,
        Hourly
    }

    public enum JobStatus
    {
        Draft,
        Open,
        InProgress,
        WaitingForReview,
        Completed,
        Overdue,
        Cancelled
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
