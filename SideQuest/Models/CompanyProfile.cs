using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideQuest.Models
{
    public class CompanyProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

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

        [MaxLength(200)]
        public string? LegalCompanyName { get; set; }

        [MaxLength(100)]
        public string? RegistrationNumber { get; set; }

        [MaxLength(100)]
        public string? TaxNumber { get; set; }

        [MaxLength(200)]
        public string? AuthorizedRepresentativeName { get; set; }

        [MaxLength(100)]
        public string? AuthorizedRepresentativeNationalId { get; set; }

        [MaxLength(40)]
        public string? PhoneNumber { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

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

        public bool IsVerified { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        public virtual ICollection<Job> Jobs { get; set; } = new HashSet<Job>();

        public virtual ICollection<CompanySubscription> CompanySubscriptions { get; set; } = new HashSet<CompanySubscription>();

        public virtual ICollection<Commission> Commissions { get; set; } = new HashSet<Commission>();
    }
}
