using System.ComponentModel.DataAnnotations;

namespace SideQuest.Models
{
    public class Achievement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public int XPRequired { get; set; }

        [Required]
        [MaxLength(500)]
        public string BadgeImageUrl { get; set; } = string.Empty;

        public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new HashSet<UserAchievement>();
    }
}
