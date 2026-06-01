using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SideQuest.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public int CategoryId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public BudgetType BudgetType { get; set; }

        [Precision(18, 2)]
        public decimal FixedBudget { get; set; }

        [Precision(18, 2)]
        public decimal HourlyRate { get; set; }

        public int WorkersNeeded { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public JobStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CompanyId))]
        public virtual CompanyProfile Company { get; set; } = null!;

        [ForeignKey(nameof(CategoryId))]
        public virtual Category Category { get; set; } = null!;

        public virtual ICollection<JobApplication> Applications { get; set; } = new HashSet<JobApplication>();

        public virtual ICollection<JobAssignment> Assignments { get; set; } = new HashSet<JobAssignment>();

        public virtual ICollection<Review> Reviews { get; set; } = new HashSet<Review>();

        public virtual ICollection<Transaction> Transactions { get; set; } = new HashSet<Transaction>();

        public virtual Commission? Commission { get; set; }
    }
}
