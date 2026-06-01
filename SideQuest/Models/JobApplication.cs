using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideQuest.Models
{
    public class JobApplication
    {
        [Key]
        public int Id { get; set; }

        public int JobId { get; set; }

        [Required]
        public string WorkerId { get; set; } = string.Empty;

        [Required]
        public string CoverLetter { get; set; } = string.Empty;

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(JobId))]
        public virtual Job Job { get; set; } = null!;

        [ForeignKey(nameof(WorkerId))]
        public virtual ApplicationUser Worker { get; set; } = null!;
    }
}
