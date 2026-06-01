using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SideQuest.Models
{
    public class Wallet
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal CurrentBalance { get; set; }

        [Precision(18, 2)]
        public decimal TotalEarned { get; set; }

        [Precision(18, 2)]
        public decimal TotalWithdrawn { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
