using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SideQuest.Models
{
    public class SubscriptionPlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal Price { get; set; }

        public int JobLimitPerMonth { get; set; }

        [Precision(5, 2)]
        public decimal CommissionRate { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public virtual ICollection<CompanySubscription> CompanySubscriptions { get; set; } = new HashSet<CompanySubscription>();
    }
}
