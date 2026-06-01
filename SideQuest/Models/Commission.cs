using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SideQuest.Models
{
    public class Commission
    {
        [Key]
        public int Id { get; set; }

        public int JobId { get; set; }

        public int CompanyId { get; set; }

        [Precision(5, 2)]
        public decimal CommissionRate { get; set; }

        [Precision(18, 2)]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(JobId))]
        public virtual Job Job { get; set; } = null!;

        [ForeignKey(nameof(CompanyId))]
        public virtual CompanyProfile Company { get; set; } = null!;
    }
}
