using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using SideQuest.Authorization;
using SideQuest.Contracts;
using SideQuest.Data;
using SideQuest.Data.Seed;
using SideQuest.Models;
using SideQuest.Services;

namespace SideQuest.Tests
{
    public class MvpWorkflowTests
    {
        [Fact]
        public async Task Seeder_Is_Idempotent_For_Roles_And_Reference_Data()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Seed:AdminEmail"] = "admin@test.local",
                    ["Seed:AdminPassword"] = "Admin123!"
                })
                .Build());
            services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
            services.AddScoped<IDataSeeder, DatabaseSeeder>();

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();

            var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
            await seeder.SeedAsync();
            await seeder.SeedAsync();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(SideQuestRoles.All.Length, await context.Roles.CountAsync());
            Assert.Equal(8, await context.Categories.CountAsync());
            Assert.Equal(3, await context.SubscriptionPlans.CountAsync());
            Assert.Equal(4, await context.Achievements.CountAsync());
            Assert.True(await context.Users.AnyAsync(user => user.Email == "admin@test.local"));
        }

        [Fact]
        public async Task Worker_Cannot_Apply_Twice_To_Same_Job()
        {
            await using var context = await CreateMarketplaceContextAsync();
            var service = new ApplicationService(context);

            var first = await service.ApplyAsync("worker-1", 1, new CreateApplicationRequest { CoverLetter = "I can help." });
            var second = await service.ApplyAsync("worker-1", 1, new CreateApplicationRequest { CoverLetter = "I can still help." });

            Assert.True(first.Succeeded);
            Assert.Equal(ServiceResultStatus.Conflict, second.Status);
            Assert.Single(context.JobApplications);
        }

        [Fact]
        public async Task Employer_Cannot_Accept_More_Workers_Than_Needed()
        {
            await using var context = await CreateMarketplaceContextAsync(workersNeeded: 1);
            context.JobApplications.AddRange(
                new JobApplication { Id = 1, JobId = 1, WorkerId = "worker-1", CoverLetter = "First", Status = ApplicationStatus.Pending },
                new JobApplication { Id = 2, JobId = 1, WorkerId = "worker-2", CoverLetter = "Second", Status = ApplicationStatus.Pending });
            await context.SaveChangesAsync();

            var service = new ApplicationService(context);

            var first = await service.AcceptAsync("employer-1", 1);
            var second = await service.AcceptAsync("employer-1", 2);

            Assert.True(first.Succeeded);
            Assert.Equal(ServiceResultStatus.Conflict, second.Status);
            Assert.Single(context.JobAssignments);
        }

        [Fact]
        public async Task Non_Owner_Cannot_Update_Job()
        {
            await using var context = await CreateMarketplaceContextAsync();
            var service = new JobService(context);

            var result = await service.UpdateJobAsync("employer-2", 1, ValidJobRequest());

            Assert.Equal(ServiceResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task Completing_Assignment_Creates_Ledger_Xp_Achievements_And_Notifications()
        {
            await using var context = await CreateMarketplaceContextAsync(workersNeeded: 1, jobStatus: JobStatus.InProgress);
            context.Achievements.Add(new Achievement
            {
                Id = 1,
                Name = "First Gig",
                Description = "Complete one job.",
                XPRequired = 100,
                BadgeImageUrl = "/badge.png"
            });
            context.JobAssignments.Add(new JobAssignment
            {
                Id = 1,
                JobId = 1,
                WorkerId = "worker-1",
                AgreedRate = 100m
            });
            await context.SaveChangesAsync();

            var service = new AssignmentService(context);

            var result = await service.CompleteAsync("employer-1", 1, new CompleteAssignmentRequest());

            Assert.True(result.Succeeded);
            Assert.Equal(JobStatus.WaitingForReview, (await context.Jobs.FindAsync(1))!.Status);
            Assert.Equal(100m, (await context.Wallets.SingleAsync(wallet => wallet.UserId == "worker-1")).CurrentBalance);
            Assert.Equal(2, await context.Transactions.CountAsync());
            Assert.Equal(12m, (await context.Commissions.SingleAsync()).Amount);
            Assert.Equal(100, (await context.UserXPs.SingleAsync(xp => xp.UserId == "worker-1")).XP);
            Assert.True(await context.UserAchievements.AnyAsync(achievement => achievement.UserId == "worker-1" && achievement.AchievementId == 1));
            Assert.Equal(2, await context.Notifications.CountAsync(notification => notification.Type == "AssignmentCompleted"));
        }

        [Fact]
        public async Task Reviews_Are_Blocked_Before_Completion_And_Allowed_After()
        {
            await using var context = await CreateMarketplaceContextAsync(workersNeeded: 1, jobStatus: JobStatus.InProgress);
            context.JobAssignments.Add(new JobAssignment
            {
                Id = 1,
                JobId = 1,
                WorkerId = "worker-1",
                AgreedRate = 100m
            });
            await context.SaveChangesAsync();

            var reviewService = new ReviewService(context);
            var earlyReview = await reviewService.CreateReviewAsync("employer-1", new CreateReviewRequest
            {
                JobId = 1,
                ReviewedUserId = "worker-1",
                Rating = 5,
                Comment = "Great work."
            });

            await new AssignmentService(context).CompleteAsync("employer-1", 1, new CompleteAssignmentRequest());

            var completedReview = await reviewService.CreateReviewAsync("employer-1", new CreateReviewRequest
            {
                JobId = 1,
                ReviewedUserId = "worker-1",
                Rating = 5,
                Comment = "Great work."
            });

            Assert.Equal(ServiceResultStatus.Conflict, earlyReview.Status);
            Assert.Equal(ServiceResultStatus.Created, completedReview.Status);
            Assert.Equal(5m, (await context.WorkerProfiles.SingleAsync(profile => profile.UserId == "worker-1")).AverageRating);
        }

        [Fact]
        public async Task Public_Job_Search_Returns_Open_Jobs_Only()
        {
            await using var context = await CreateMarketplaceContextAsync();
            context.Jobs.Add(new Job
            {
                Id = 2,
                CompanyId = 1,
                CategoryId = 1,
                Title = "Completed job",
                Description = "Closed",
                BudgetType = BudgetType.Fixed,
                FixedBudget = 50m,
                WorkersNeeded = 1,
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Status = JobStatus.Completed
            });
            await context.SaveChangesAsync();

            var result = await new JobService(context).GetOpenJobsAsync(new JobQueryParameters { Status = JobStatus.Completed });

            Assert.True(result.Succeeded);
            Assert.Single(result.Value!);
            Assert.Equal(JobStatus.Open, result.Value![0].Status);
        }

        private static async Task<AppDbContext> CreateMarketplaceContextAsync(
            int workersNeeded = 2,
            JobStatus jobStatus = JobStatus.Open)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            var now = DateTime.UtcNow;

            context.Users.AddRange(
                new ApplicationUser { Id = "employer-1", UserName = "employer1@test.local", Email = "employer1@test.local", FullName = "Employer One" },
                new ApplicationUser { Id = "employer-2", UserName = "employer2@test.local", Email = "employer2@test.local", FullName = "Employer Two" },
                new ApplicationUser { Id = "worker-1", UserName = "worker1@test.local", Email = "worker1@test.local", FullName = "Worker One" },
                new ApplicationUser { Id = "worker-2", UserName = "worker2@test.local", Email = "worker2@test.local", FullName = "Worker Two" });

            context.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Id = 1,
                Name = "Free",
                Price = 0m,
                JobLimitPerMonth = 3,
                CommissionRate = 12m,
                Description = "Free plan"
            });
            context.Categories.Add(new Category { Id = 1, Name = "Delivery", Description = "Delivery jobs", IsActive = true, CreatedAt = now });
            context.CompanyProfiles.Add(new CompanyProfile
            {
                Id = 1,
                UserId = "employer-1",
                CompanyName = "Quest Co",
                Description = "Temporary work",
                Location = "Amman",
                CreatedAt = now
            });
            context.CompanySubscriptions.Add(new CompanySubscription
            {
                Id = 1,
                CompanyId = 1,
                PlanId = 1,
                StartDate = now.AddDays(-1),
                EndDate = now.AddMonths(1),
                IsActive = true
            });
            context.WorkerProfiles.AddRange(
                new WorkerProfile { Id = 1, UserId = "worker-1", Headline = "Reliable", Bio = "Ready", Location = "Amman" },
                new WorkerProfile { Id = 2, UserId = "worker-2", Headline = "Fast", Bio = "Ready", Location = "Amman" });
            context.Jobs.Add(new Job
            {
                Id = 1,
                CompanyId = 1,
                CategoryId = 1,
                Title = "Delivery helper",
                Description = "Help with delivery.",
                BudgetType = BudgetType.Fixed,
                FixedBudget = 200m,
                WorkersNeeded = workersNeeded,
                StartDate = now.AddDays(1),
                EndDate = now.AddDays(2),
                Status = jobStatus,
                CreatedAt = now
            });

            await context.SaveChangesAsync();
            return context;
        }

        private static UpsertJobRequest ValidJobRequest()
        {
            return new UpsertJobRequest
            {
                Title = "Updated delivery helper",
                Description = "Updated details.",
                CategoryId = 1,
                BudgetType = BudgetType.Fixed,
                FixedBudget = 200m,
                WorkersNeeded = 2,
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2)
            };
        }

        private sealed class TestWebHostEnvironment : IWebHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Development";

            public string ApplicationName { get; set; } = "SideQuest.Tests";

            public string WebRootPath { get; set; } = string.Empty;

            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

            public string ContentRootPath { get; set; } = string.Empty;

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
