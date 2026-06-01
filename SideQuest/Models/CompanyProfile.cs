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
