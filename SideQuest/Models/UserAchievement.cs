using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SideQuest.Models
{
    [PrimaryKey(nameof(UserId), nameof(AchievementId))]
    public class UserAchievement
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        public int AchievementId { get; set; }

        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey(nameof(AchievementId))]
        public virtual Achievement Achievement { get; set; } = null!;
    }
}
