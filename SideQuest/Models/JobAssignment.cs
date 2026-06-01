using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SideQuest.Models
{
    public class JobAssignment
    {
        [Key]
        public int Id { get; set; }

        public int JobId { get; set; }

        [Required]
        public string WorkerId { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal AgreedRate { get; set; }

        [Precision(10, 2)]
        public decimal HoursWorked { get; set; }

        [Precision(18, 2)]
        public decimal Earnings { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedAt { get; set; }

        [ForeignKey(nameof(JobId))]
        public virtual Job Job { get; set; } = null!;

        [ForeignKey(nameof(WorkerId))]
        public virtual ApplicationUser Worker { get; set; } = null!;
    }
}
