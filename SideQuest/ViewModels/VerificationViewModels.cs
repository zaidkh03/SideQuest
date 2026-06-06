using System.ComponentModel.DataAnnotations;
using SideQuest.Models;

namespace SideQuest.ViewModels
{
    public enum OnboardingAccountType
    {
        Worker,
        Company
    }

    public sealed class OnboardingStatusViewModel
    {
        public OnboardingAccountType? AccountType { get; set; }

        public VerificationStatus Status { get; set; } = VerificationStatus.Draft;

        public string DisplayName { get; set; } = string.Empty;

        public string? RejectionReason { get; set; }

        public string? RejectionMessage { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string? ContinueAction { get; set; }
    }

    public sealed class WorkerVerificationFormViewModel
    {
        [Required]
        [MaxLength(200)]
        [Display(Name = "Professional headline")]
        public string Headline { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Short bio")]
        public string Bio { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Range(0, 1000000)]
        [Display(Name = "Preferred hourly rate")]
        public decimal? HourlyRatePreference { get; set; }

        public AvailabilityStatus AvailabilityStatus { get; set; } = AvailabilityStatus.Available;

        [MaxLength(500)]
        [Display(Name = "Portfolio URL")]
        public string? PortfolioUrl { get; set; }

        [MaxLength(500)]
        [Display(Name = "Resume URL")]
        public string? ResumeUrl { get; set; }

        [Range(0, 80)]
        [Display(Name = "Experience years")]
        public int ExperienceYears { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "Legal full name")]
        public string LegalName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "National ID")]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        [Phone]
        [MaxLength(40)]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Country of residence")]
        public string ResidenceCountry { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        [Display(Name = "City of residence")]
        public string ResidenceCity { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? VerificationDateOfBirth { get; set; }

        [MaxLength(500)]
        [Display(Name = "Document reference")]
        public string? VerificationDocumentPath { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Verification notes")]
        public string? VerificationNotes { get; set; }
    }

    public sealed class CompanyVerificationFormViewModel
    {
        [Required]
        [MaxLength(200)]
        [Display(Name = "Public company name")]
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

        [Required]
        [MaxLength(200)]
        [Display(Name = "Legal company name")]
        public string LegalCompanyName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Registration number")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Tax ID")]
        public string? TaxNumber { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "Authorized representative")]
        public string AuthorizedRepresentativeName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Representative national ID")]
        public string AuthorizedRepresentativeNationalId { get; set; } = string.Empty;

        [Required]
        [Phone]
        [MaxLength(40)]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Document reference")]
        public string? VerificationDocumentPath { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Verification notes")]
        public string? VerificationNotes { get; set; }
    }

    public sealed class VerificationQueueViewModel
    {
        public int SubmittedWorkers { get; set; }

        public int SubmittedCompanies { get; set; }

        public int RejectedRequests { get; set; }

        public int ApprovedRequests { get; set; }

        public IReadOnlyList<WorkerVerificationQueueItemViewModel> Workers { get; set; } = [];

        public IReadOnlyList<CompanyVerificationQueueItemViewModel> Companies { get; set; } = [];
    }

    public sealed class WorkerVerificationQueueItemViewModel
    {
        public int ProfileId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Headline { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string MaskedNationalId { get; set; } = string.Empty;

        public VerificationStatus Status { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }
    }

    public sealed class CompanyVerificationQueueItemViewModel
    {
        public int ProfileId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string RegistrationNumber { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public VerificationStatus Status { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }
    }

    public sealed class WorkerVerificationReviewViewModel
    {
        public int ProfileId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Headline { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string LegalName { get; set; } = string.Empty;

        public string MaskedNationalId { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Residence { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string? VerificationDocumentPath { get; set; }

        public string? VerificationNotes { get; set; }

        public VerificationStatus Status { get; set; }

        public DateTime? SubmittedAt { get; set; }
    }

    public sealed class CompanyVerificationReviewViewModel
    {
        public int ProfileId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string LegalCompanyName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string RegistrationNumber { get; set; } = string.Empty;

        public string? TaxNumber { get; set; }

        public string AuthorizedRepresentativeName { get; set; } = string.Empty;

        public string MaskedRepresentativeNationalId { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string? Website { get; set; }

        public string? VerificationDocumentPath { get; set; }

        public string? VerificationNotes { get; set; }

        public VerificationStatus Status { get; set; }

        public DateTime? SubmittedAt { get; set; }
    }

    public sealed class VerificationDecisionViewModel
    {
        [Required]
        [MaxLength(200)]
        public string RejectionReason { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? RejectionMessage { get; set; }
    }
}
