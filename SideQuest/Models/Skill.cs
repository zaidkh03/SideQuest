using System.ComponentModel.DataAnnotations;

namespace SideQuest.Models
{
    public class Skill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<UserSkill> UserSkills { get; set; } = new HashSet<UserSkill>();
    }
}
