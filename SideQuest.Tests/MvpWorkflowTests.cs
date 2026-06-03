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

        [Fact]
        public async Task Worker_Verification_Grants_Role_Only_After_Admin_Approval()
        {
            await using var provider = await CreateApprovalServiceProviderAsync();
            await using var scope = provider.CreateAsyncScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
            var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var worker = new ApplicationUser
            {
                UserName = "pending-worker@test.local",
                Email = "pending-worker@test.local",
                FullName = "Pending Worker"
            };
            Assert.True((await userManager.CreateAsync(worker, "Worker123!")).Succeeded);

            var submit = await profileService.SubmitWorkerVerificationAsync(worker.Id, new SubmitWorkerVerificationRequest
            {
                Headline = "Event helper",
                Bio = "Ready for short-term quests.",
                Location = "Amman",
                AvailabilityStatus = AvailabilityStatus.Available,
                ExperienceYears = 2,
                LegalName = "Pending Worker",
                NationalId = "9876543210",
                PhoneNumber = "+962790000000",
                ResidenceCountry = "Jordan",
                ResidenceCity = "Amman",
                VerificationDateOfBirth = new DateTime(2000, 1, 1)
            });

            Assert.True(submit.Succeeded);
            Assert.False(await userManager.IsInRoleAsync(worker, SideQuestRoles.Worker));
            Assert.Equal(VerificationStatus.Submitted, (await context.WorkerProfiles.SingleAsync()).VerificationStatus);

            var admin = new ApplicationUser
            {
                UserName = "approval-admin@test.local",
                Email = "approval-admin@test.local",
                FullName = "Approval Admin"
            };
            Assert.True((await userManager.CreateAsync(admin, "Admin123!")).Succeeded);
            await userManager.AddToRoleAsync(admin, SideQuestRoles.Admin);

            var profileId = await context.WorkerProfiles.Select(profile => profile.Id).SingleAsync();
            var approval = await adminService.ApproveWorkerVerificationAsync(profileId, admin.Id);

            Assert.True(approval.Succeeded);
            Assert.True(await userManager.IsInRoleAsync(worker, SideQuestRoles.Worker));
            Assert.Equal(VerificationStatus.Approved, (await context.WorkerProfiles.SingleAsync()).VerificationStatus);
            Assert.True(await context.Wallets.AnyAsync(wallet => wallet.UserId == worker.Id));
            Assert.True(await context.UserXPs.AnyAsync(xp => xp.UserId == worker.Id));
        }

        [Fact]
        public async Task Company_Verification_Grants_Employer_Role_And_Subscription_Only_After_Approval()
        {
            await using var provider = await CreateApprovalServiceProviderAsync();
            await using var scope = provider.CreateAsyncScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
            var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Name = "Free",
                Price = 0m,
                JobLimitPerMonth = 3,
                CommissionRate = 12m,
                Description = "Free plan"
            });
            await context.SaveChangesAsync();

            var companyUser = new ApplicationUser
            {
                UserName = "pending-company@test.local",
                Email = "pending-company@test.local",
                FullName = "Pending Company Rep"
            };
            Assert.True((await userManager.CreateAsync(companyUser, "Company123!")).Succeeded);

            var submit = await profileService.SubmitCompanyVerificationAsync(companyUser.Id, new SubmitCompanyVerificationRequest
            {
                CompanyName = "Pending Co",
                Description = "A company waiting for review.",
                Location = "Amman",
                LegalCompanyName = "Pending Company LLC",
                RegistrationNumber = "REG-123456",
                AuthorizedRepresentativeName = "Pending Company Rep",
                AuthorizedRepresentativeNationalId = "1234567890",
                PhoneNumber = "+962780000000",
                Address = "Amman, Jordan"
            });

            Assert.True(submit.Succeeded);
            Assert.False(await userManager.IsInRoleAsync(companyUser, SideQuestRoles.Employer));
            Assert.False((await context.CompanyProfiles.SingleAsync()).IsVerified);

            var admin = new ApplicationUser
            {
                UserName = "company-admin@test.local",
                Email = "company-admin@test.local",
                FullName = "Company Admin"
            };
            Assert.True((await userManager.CreateAsync(admin, "Admin123!")).Succeeded);
            await userManager.AddToRoleAsync(admin, SideQuestRoles.Admin);

            var profileId = await context.CompanyProfiles.Select(profile => profile.Id).SingleAsync();
            var approval = await adminService.ApproveCompanyVerificationAsync(profileId, admin.Id);

            Assert.True(approval.Succeeded);
            Assert.True(await userManager.IsInRoleAsync(companyUser, SideQuestRoles.Employer));
            Assert.True((await context.CompanyProfiles.SingleAsync()).IsVerified);
            Assert.True(await context.CompanySubscriptions.AnyAsync(subscription => subscription.IsActive));
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

        private static async Task<ServiceProvider> CreateApprovalServiceProviderAsync()
        {
            var services = new ServiceCollection();
            var databaseName = Guid.NewGuid().ToString();
            services.AddLogging();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IAdminService, AdminService>();

            var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var role in SideQuestRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            return provider;
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
