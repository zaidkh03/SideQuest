using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SideQuest.Models
{
    public class WorkerProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Headline { get; set; } = string.Empty;

        [Required]
        public string Bio { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal? HourlyRatePreference { get; set; }

        public AvailabilityStatus AvailabilityStatus { get; set; }

        [MaxLength(200)]
        public string? LegalName { get; set; }

        [MaxLength(100)]
        public string? NationalId { get; set; }

        [MaxLength(40)]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        public string? ResidenceCountry { get; set; }

        [MaxLength(120)]
        public string? ResidenceCity { get; set; }

        public DateTime? VerificationDateOfBirth { get; set; }

        [MaxLength(500)]
        public string? VerificationDocumentPath { get; set; }

        [MaxLength(1000)]
        public string? VerificationNotes { get; set; }

        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Draft;

        public DateTime? VerificationSubmittedAt { get; set; }

        public DateTime? VerificationReviewedAt { get; set; }

        [MaxLength(450)]
        public string? VerificationReviewedByAdminId { get; set; }

        [MaxLength(200)]
        public string? VerificationRejectionReason { get; set; }

        [MaxLength(1000)]
        public string? VerificationRejectionMessage { get; set; }

        [MaxLength(500)]
        public string? PortfolioUrl { get; set; }

        [MaxLength(500)]
        public string? ResumeUrl { get; set; }

        public int ExperienceYears { get; set; }

        public int TotalJobsCompleted { get; set; }

        [Precision(3, 2)]
        public decimal AverageRating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
