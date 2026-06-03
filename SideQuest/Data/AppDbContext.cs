using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SideQuest.Models;

namespace SideQuest.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<WorkerProfile> WorkerProfiles { get; set; }

        public DbSet<CompanyProfile> CompanyProfiles { get; set; }

        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

        public DbSet<CompanySubscription> CompanySubscriptions { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Job> Jobs { get; set; }

        public DbSet<JobApplication> JobApplications { get; set; }

        public DbSet<JobAssignment> JobAssignments { get; set; }

        public DbSet<Review> Reviews { get; set; }

        public DbSet<BankAccount> BankAccounts { get; set; }

        public DbSet<Wallet> Wallets { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<Commission> Commissions { get; set; }

        public DbSet<UserXP> UserXPs { get; set; }

        public DbSet<Achievement> Achievements { get; set; }

        public DbSet<UserAchievement> UserAchievements { get; set; }

        public DbSet<Skill> Skills { get; set; }

        public DbSet<UserSkill> UserSkills { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<CommunityPost> CommunityPosts { get; set; }

        public DbSet<CommunityComment> CommunityComments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            ConfigureUsers(builder);
            ConfigureProfiles(builder);
            ConfigureSubscriptions(builder);
            ConfigureJobs(builder);
            ConfigureReviews(builder);
            ConfigureFinancials(builder);
            ConfigureGamification(builder);
            ConfigureSkills(builder);
            ConfigureNotifications(builder);
            ConfigureCommunity(builder);
            ConfigureCategories(builder);
        }

        private static void ConfigureUsers(ModelBuilder builder)
        {
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(user => user.FullName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(user => user.ProfileImageUrl)
                    .HasMaxLength(500);
            });
        }

        private static void ConfigureProfiles(ModelBuilder builder)
        {
            builder.Entity<WorkerProfile>(entity =>
            {
                entity.HasIndex(profile => profile.UserId)
                    .IsUnique();

                entity.Property(profile => profile.Headline)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(profile => profile.Bio)
                    .IsRequired();

                entity.Property(profile => profile.Location)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(profile => profile.HourlyRatePreference)
                    .HasPrecision(18, 2);

                entity.Property(profile => profile.AverageRating)
                    .HasPrecision(3, 2);

                entity.HasIndex(profile => profile.VerificationStatus);

                entity.Property(profile => profile.PortfolioUrl)
                    .HasMaxLength(500);

                entity.Property(profile => profile.ResumeUrl)
                    .HasMaxLength(500);

                entity.HasOne(profile => profile.User)
                    .WithOne(user => user.WorkerProfile)
                    .HasForeignKey<WorkerProfile>(profile => profile.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CompanyProfile>(entity =>
            {
                entity.HasIndex(profile => profile.UserId)
                    .IsUnique();

                entity.Property(profile => profile.CompanyName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(profile => profile.Description)
                    .IsRequired();

                entity.Property(profile => profile.Location)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(profile => profile.Website)
                    .HasMaxLength(300);

                entity.Property(profile => profile.LogoUrl)
                    .HasMaxLength(500);

                entity.HasIndex(profile => profile.VerificationStatus);

                entity.HasOne(profile => profile.User)
                    .WithOne(user => user.CompanyProfile)
                    .HasForeignKey<CompanyProfile>(profile => profile.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureSubscriptions(ModelBuilder builder)
        {
            builder.Entity<SubscriptionPlan>(entity =>
            {
                entity.HasIndex(plan => plan.Name)
                    .IsUnique();

                entity.Property(plan => plan.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(plan => plan.Price)
                    .HasPrecision(18, 2);

                entity.Property(plan => plan.CommissionRate)
                    .HasPrecision(18, 2);

                entity.Property(plan => plan.Description)
                    .IsRequired()
                    .HasMaxLength(1000);
            });

            builder.Entity<CompanySubscription>(entity =>
            {
                entity.HasOne(subscription => subscription.Company)
                    .WithMany(company => company.CompanySubscriptions)
                    .HasForeignKey(subscription => subscription.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(subscription => subscription.Plan)
                    .WithMany(plan => plan.CompanySubscriptions)
                    .HasForeignKey(subscription => subscription.PlanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureJobs(ModelBuilder builder)
        {
            builder.Entity<Job>(entity =>
            {
                entity.HasIndex(job => job.CompanyId);

                entity.HasIndex(job => job.Status);

                entity.HasIndex(job => job.CategoryId);

                entity.HasIndex(job => new
                {
                    job.Status,
                    job.CategoryId,
                    job.StartDate
                });

                entity.Property(job => job.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(job => job.Description)
                    .IsRequired();

                entity.Property(job => job.FixedBudget)
                    .HasPrecision(18, 2);

                entity.Property(job => job.HourlyRate)
                    .HasPrecision(18, 2);

                entity.HasOne(job => job.Company)
                    .WithMany(company => company.Jobs)
                    .HasForeignKey(job => job.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(job => job.Category)
                    .WithMany(category => category.Jobs)
                    .HasForeignKey(job => job.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<JobApplication>(entity =>
            {
                entity.HasIndex(application => new
                    {
                        application.JobId,
                        application.WorkerId
                    })
                    .IsUnique();

                entity.Property(application => application.CoverLetter)
                    .IsRequired();

                entity.HasOne(application => application.Job)
                    .WithMany(job => job.Applications)
                    .HasForeignKey(application => application.JobId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(application => application.Worker)
                    .WithMany(user => user.JobApplications)
                    .HasForeignKey(application => application.WorkerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<JobAssignment>(entity =>
            {
                entity.HasIndex(assignment => new
                    {
                        assignment.JobId,
                        assignment.WorkerId
                    })
                    .IsUnique();

                entity.Property(assignment => assignment.AgreedRate)
                    .HasPrecision(18, 2);

                entity.Property(assignment => assignment.HoursWorked)
                    .HasPrecision(18, 2);

                entity.Property(assignment => assignment.Earnings)
                    .HasPrecision(18, 2);

                entity.HasOne(assignment => assignment.Job)
                    .WithMany(job => job.Assignments)
                    .HasForeignKey(assignment => assignment.JobId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(assignment => assignment.Worker)
                    .WithMany(user => user.JobAssignments)
                    .HasForeignKey(assignment => assignment.WorkerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureReviews(ModelBuilder builder)
        {
            builder.Entity<Review>(entity =>
            {
                entity.HasIndex(review => new
                    {
                        review.JobId,
                        review.ReviewerId,
                        review.ReviewedUserId
                    })
                    .IsUnique();

                entity.Property(review => review.Comment)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.HasOne(review => review.Job)
                    .WithMany(job => job.Reviews)
                    .HasForeignKey(review => review.JobId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(review => review.Reviewer)
                    .WithMany(user => user.ReviewsGiven)
                    .HasForeignKey(review => review.ReviewerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(review => review.ReviewedUser)
                    .WithMany(user => user.ReviewsReceived)
                    .HasForeignKey(review => review.ReviewedUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureFinancials(ModelBuilder builder)
        {
            builder.Entity<Wallet>(entity =>
            {
                entity.HasIndex(wallet => wallet.UserId)
                    .IsUnique();

                entity.Property(wallet => wallet.CurrentBalance)
                    .HasPrecision(18, 2);

                entity.Property(wallet => wallet.TotalEarned)
                    .HasPrecision(18, 2);

                entity.Property(wallet => wallet.TotalWithdrawn)
                    .HasPrecision(18, 2);

                entity.HasOne(wallet => wallet.User)
                    .WithOne(user => user.Wallet)
                    .HasForeignKey<Wallet>(wallet => wallet.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<BankAccount>(entity =>
            {
                entity.Property(account => account.AccountHolderName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(account => account.BankName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(account => account.IBAN)
                    .IsRequired()
                    .HasMaxLength(34);

                entity.Property(account => account.AccountNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasOne(account => account.User)
                    .WithMany(user => user.BankAccounts)
                    .HasForeignKey(account => account.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(transaction => transaction.UserId);

                entity.HasIndex(transaction => transaction.JobId);

                entity.Property(transaction => transaction.Amount)
                    .HasPrecision(18, 2);

                entity.HasOne(transaction => transaction.User)
                    .WithMany(user => user.Transactions)
                    .HasForeignKey(transaction => transaction.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(transaction => transaction.Job)
                    .WithMany(job => job.Transactions)
                    .HasForeignKey(transaction => transaction.JobId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Commission>(entity =>
            {
                entity.Property(commission => commission.CommissionRate)
                    .HasPrecision(18, 2);

                entity.Property(commission => commission.Amount)
                    .HasPrecision(18, 2);

                entity.HasOne(commission => commission.Job)
                    .WithOne(job => job.Commission)
                    .HasForeignKey<Commission>(commission => commission.JobId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(commission => commission.Company)
                    .WithMany(company => company.Commissions)
                    .HasForeignKey(commission => commission.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureGamification(ModelBuilder builder)
        {
            builder.Entity<UserXP>(entity =>
            {
                entity.HasKey(userXP => userXP.UserId);

                entity.HasOne(userXP => userXP.User)
                    .WithOne(user => user.UserXP)
                    .HasForeignKey<UserXP>(userXP => userXP.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Achievement>(entity =>
            {
                entity.HasIndex(achievement => achievement.Name)
                    .IsUnique();

                entity.Property(achievement => achievement.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(achievement => achievement.Description)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(achievement => achievement.BadgeImageUrl)
                    .IsRequired()
                    .HasMaxLength(500);
            });

            builder.Entity<UserAchievement>(entity =>
            {
                entity.HasKey(userAchievement => new
                {
                    userAchievement.UserId,
                    userAchievement.AchievementId
                });

                entity.HasOne(userAchievement => userAchievement.User)
                    .WithMany(user => user.UserAchievements)
                    .HasForeignKey(userAchievement => userAchievement.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(userAchievement => userAchievement.Achievement)
                    .WithMany(achievement => achievement.UserAchievements)
                    .HasForeignKey(userAchievement => userAchievement.AchievementId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureSkills(ModelBuilder builder)
        {
            builder.Entity<Skill>(entity =>
            {
                entity.HasIndex(skill => skill.Name)
                    .IsUnique();

                entity.Property(skill => skill.Name)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            builder.Entity<UserSkill>(entity =>
            {
                entity.HasKey(userSkill => new
                {
                    userSkill.UserId,
                    userSkill.SkillId
                });

                entity.HasOne(userSkill => userSkill.User)
                    .WithMany(user => user.UserSkills)
                    .HasForeignKey(userSkill => userSkill.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(userSkill => userSkill.Skill)
                    .WithMany(skill => skill.UserSkills)
                    .HasForeignKey(userSkill => userSkill.SkillId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureNotifications(ModelBuilder builder)
        {
            builder.Entity<Notification>(entity =>
            {
                entity.Property(notification => notification.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(notification => notification.Message)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(notification => notification.Type)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(notification => notification.User)
                    .WithMany(user => user.Notifications)
                    .HasForeignKey(notification => notification.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureCommunity(ModelBuilder builder)
        {
            builder.Entity<CommunityPost>(entity =>
            {
                entity.Property(post => post.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(post => post.Content)
                    .IsRequired();

                entity.HasOne(post => post.User)
                    .WithMany(user => user.CommunityPosts)
                    .HasForeignKey(post => post.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CommunityComment>(entity =>
            {
                entity.Property(comment => comment.Content)
                    .IsRequired();

                entity.HasOne(comment => comment.Post)
                    .WithMany(post => post.Comments)
                    .HasForeignKey(comment => comment.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(comment => comment.User)
                    .WithMany(user => user.CommunityComments)
                    .HasForeignKey(comment => comment.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureCategories(ModelBuilder builder)
        {
            builder.Entity<Category>(entity =>
            {
                entity.HasIndex(category => category.Name)
                    .IsUnique();

                entity.Property(category => category.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(category => category.Description)
                    .IsRequired()
                    .HasMaxLength(1000);
            });
        }
    }
}
