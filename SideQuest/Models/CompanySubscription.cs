using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideQuest.Models
{
    public class CompanySubscription
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public int PlanId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public virtual CompanyProfile Company { get; set; } = null!;

        [ForeignKey(nameof(PlanId))]
        public virtual SubscriptionPlan Plan { get; set; } = null!;
    }
}
