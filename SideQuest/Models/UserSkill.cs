using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SideQuest.Models
{
    [PrimaryKey(nameof(UserId), nameof(SkillId))]
    public class UserSkill
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        public int SkillId { get; set; }

        public int SkillLevel { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey(nameof(SkillId))]
        public virtual Skill Skill { get; set; } = null!;
    }
}
