using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideQuest.Models
{
    public class UserXP
    {
        [Key]
        [Required]
        public string UserId { get; set; } = string.Empty;

        public int XP { get; set; }

        public int Level { get; set; } = 1;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
