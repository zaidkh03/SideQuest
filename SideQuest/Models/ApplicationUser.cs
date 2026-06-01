using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SideQuest.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ProfileImageUrl { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginAt { get; set; }

        public virtual WorkerProfile? WorkerProfile { get; set; }

        public virtual CompanyProfile? CompanyProfile { get; set; }

        public virtual UserXP? UserXP { get; set; }

        public virtual ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();

        public virtual Wallet? Wallet { get; set; }

        public virtual ICollection<BankAccount> BankAccounts { get; set; } = new HashSet<BankAccount>();

        public virtual ICollection<Transaction> Transactions { get; set; } = new HashSet<Transaction>();

        public virtual ICollection<JobApplication> JobApplications { get; set; } = new HashSet<JobApplication>();

        public virtual ICollection<JobAssignment> JobAssignments { get; set; } = new HashSet<JobAssignment>();

        [InverseProperty(nameof(Review.Reviewer))]
        public virtual ICollection<Review> ReviewsGiven { get; set; } = new HashSet<Review>();

        [InverseProperty(nameof(Review.ReviewedUser))]
        public virtual ICollection<Review> ReviewsReceived { get; set; } = new HashSet<Review>();

        public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new HashSet<UserAchievement>();

        public virtual ICollection<UserSkill> UserSkills { get; set; } = new HashSet<UserSkill>();

        public virtual ICollection<CommunityPost> CommunityPosts { get; set; } = new HashSet<CommunityPost>();

        public virtual ICollection<CommunityComment> CommunityComments { get; set; } = new HashSet<CommunityComment>();
    }
}
