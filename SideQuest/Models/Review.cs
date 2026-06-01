using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideQuest.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        public int JobId { get; set; }

        [Required]
        public string ReviewerId { get; set; } = string.Empty;

        [Required]
        public string ReviewedUserId { get; set; } = string.Empty;

        public int Rating { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(JobId))]
        public virtual Job Job { get; set; } = null!;

        [ForeignKey(nameof(ReviewerId))]
        [InverseProperty(nameof(ApplicationUser.ReviewsGiven))]
        public virtual ApplicationUser Reviewer { get; set; } = null!;

        [ForeignKey(nameof(ReviewedUserId))]
        [InverseProperty(nameof(ApplicationUser.ReviewsReceived))]
        public virtual ApplicationUser ReviewedUser { get; set; } = null!;
    }
}
