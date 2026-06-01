using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SideQuest.Authorization;
using SideQuest.Models;

namespace SideQuest.Data.Seed
{
    public class DatabaseSeeder : IDataSeeder
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public DatabaseSeeder(
            AppDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task SeedAsync()
        {
            await SeedRoles();
            await SeedDevelopmentAdmin();
            await SeedCategories();
            await SeedSubscriptionPlans();
            await SeedAchievements();
            await SeedSkills();
            await _context.SaveChangesAsync();
        }

        private async Task SeedRoles()
        {
            foreach (var role in SideQuestRoles.All)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private async Task SeedDevelopmentAdmin()
        {
            if (!_environment.IsDevelopment())
            {
                return;
            }

            var adminEmail = _configuration["Seed:AdminEmail"];
            var adminPassword = _configuration["Seed:AdminPassword"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                return;
            }

            var adminUser = await _userManager.FindByEmailAsync(adminEmail);
            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "SideQuest Admin"
                };

                var createResult = await _userManager.CreateAsync(adminUser, adminPassword);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to seed development admin: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
                }
            }

            if (!await _userManager.IsInRoleAsync(adminUser, SideQuestRoles.Admin))
            {
                await _userManager.AddToRoleAsync(adminUser, SideQuestRoles.Admin);
            }
        }

        private async Task SeedCategories()
        {
            var existingCategoryNames = await _context.Categories
                .Select(category => category.Name)
                .ToListAsync();

            var existingNames = existingCategoryNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var now = DateTime.UtcNow;

            var categories = new[]
            {
                new Category
                {
                    Name = "Sales",
                    Description = "Sales outreach, lead generation, and customer acquisition roles.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Marketing",
                    Description = "Campaign, brand, social media, and growth marketing work.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Photography",
                    Description = "Event, product, portrait, and commercial photography gigs.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Event Staff",
                    Description = "On-site event operations, ushering, check-in, and coordination support.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Customer Support",
                    Description = "Customer service, chat support, call support, and issue resolution roles.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Delivery",
                    Description = "Local delivery, pickup, courier, and logistics support gigs.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "IT Support",
                    Description = "Technical support, device setup, troubleshooting, and help desk work.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Design",
                    Description = "Graphic design, UI design, branding, and visual production work.",
                    IsActive = true,
                    CreatedAt = now
                }
            };

            await _context.Categories.AddRangeAsync(
                categories.Where(category => !existingNames.Contains(category.Name)));
        }

        private async Task SeedSubscriptionPlans()
        {
            var existingPlanNames = await _context.SubscriptionPlans
                .Select(plan => plan.Name)
                .ToListAsync();

            var existingNames = existingPlanNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var plans = new[]
            {
                new SubscriptionPlan
                {
                    Name = "Free",
                    Price = 0.00m,
                    JobLimitPerMonth = 3,
                    CommissionRate = 12.00m,
                    Description = "Starter plan for small teams testing SideQuest with up to 3 jobs per month."
                },
                new SubscriptionPlan
                {
                    Name = "Pro",
                    Price = 29.99m,
                    JobLimitPerMonth = 25,
                    CommissionRate = 8.00m,
                    Description = "Mid-tier plan for growing companies with a higher monthly job limit."
                },
                new SubscriptionPlan
                {
                    Name = "Business",
                    Price = 99.99m,
                    JobLimitPerMonth = int.MaxValue,
                    CommissionRate = 5.00m,
                    Description = "Premium plan for high-volume companies with unlimited monthly jobs."
                }
            };

            await _context.SubscriptionPlans.AddRangeAsync(
                plans.Where(plan => !existingNames.Contains(plan.Name)));
        }

        private async Task SeedAchievements()
        {
            var existingAchievementNames = await _context.Achievements
                .Select(achievement => achievement.Name)
                .ToListAsync();

            var existingNames = existingAchievementNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var achievements = new[]
            {
                new Achievement
                {
                    Name = "First Gig",
                    Description = "Complete the first SideQuest job.",
                    XPRequired = 100,
                    BadgeImageUrl = "/images/badges/first-gig.png"
                },
                new Achievement
                {
                    Name = "10 Jobs Completed",
                    Description = "Complete 10 jobs on the SideQuest platform.",
                    XPRequired = 500,
                    BadgeImageUrl = "/images/badges/ten-jobs-completed.png"
                },
                new Achievement
                {
                    Name = "Top Rated Worker",
                    Description = "Earn a consistently high worker rating from completed jobs.",
                    XPRequired = 1000,
                    BadgeImageUrl = "/images/badges/top-rated-worker.png"
                },
                new Achievement
                {
                    Name = "Community Helper",
                    Description = "Contribute helpful posts or comments in the SideQuest community.",
                    XPRequired = 250,
                    BadgeImageUrl = "/images/badges/community-helper.png"
                }
            };

            await _context.Achievements.AddRangeAsync(
                achievements.Where(achievement => !existingNames.Contains(achievement.Name)));
        }

        private async Task SeedSkills()
        {
            var existingSkillNames = await _context.Skills
                .Select(skill => skill.Name)
                .ToListAsync();

            var existingNames = existingSkillNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var skills = new[]
            {
                "Sales Outreach",
                "Lead Generation",
                "Social Media",
                "Photography",
                "Event Operations",
                "Customer Support",
                "Delivery",
                "IT Support",
                "Graphic Design",
                "Writing",
                "Data Entry",
                "Translation"
            };

            await _context.Skills.AddRangeAsync(
                skills
                    .Where(skill => !existingNames.Contains(skill))
                    .Select(skill => new Skill { Name = skill }));
        }
    }
}
