using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SideQuest.Authorization;
using SideQuest.Contracts;
using SideQuest.Data;
using SideQuest.Models;

namespace SideQuest.Services
{
    public interface IAdminService
    {
        Task<ServiceResult<IReadOnlyList<CategoryResponse>>> GetCategoriesAsync();

        Task<ServiceResult<CategoryResponse>> CreateCategoryAsync(CategoryRequest request);

        Task<ServiceResult<CategoryResponse>> UpdateCategoryAsync(int categoryId, CategoryRequest request);

        Task<ServiceResult<IReadOnlyList<AchievementResponse>>> GetAchievementsAsync();

        Task<ServiceResult<AchievementResponse>> CreateAchievementAsync(AchievementRequest request);

        Task<ServiceResult<AchievementResponse>> UpdateAchievementAsync(int achievementId, AchievementRequest request);

        Task<ServiceResult<IReadOnlyList<AdminUserResponse>>> GetUsersAsync();

        Task<ServiceResult<AdminUserResponse>> UpdateUserStatusAsync(string userId, UpdateUserStatusRequest request);

        Task<ServiceResult<bool>> ApproveWorkerVerificationAsync(int profileId, string adminUserId);

        Task<ServiceResult<bool>> RejectWorkerVerificationAsync(int profileId, string adminUserId, string reason, string? message);

        Task<ServiceResult<bool>> ApproveCompanyVerificationAsync(int profileId, string adminUserId);

        Task<ServiceResult<bool>> RejectCompanyVerificationAsync(int profileId, string adminUserId, string reason, string? message);
    }

    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ServiceResult<IReadOnlyList<CategoryResponse>>> GetCategoriesAsync()
        {
            var categories = await _context.Categories
                .OrderBy(category => category.Name)
                .ToListAsync();

            return ServiceResult<IReadOnlyList<CategoryResponse>>.Success(
                categories.Select(category => category.ToResponse()).ToList());
        }

        public async Task<ServiceResult<CategoryResponse>> CreateCategoryAsync(CategoryRequest request)
        {
            if (await CategoryNameExistsAsync(request.Name, excludedCategoryId: null))
            {
                return ServiceResult<CategoryResponse>.Conflict("A category with this name already exists.");
            }

            var category = new Category
            {
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return ServiceResult<CategoryResponse>.Created(category.ToResponse());
        }

        public async Task<ServiceResult<CategoryResponse>> UpdateCategoryAsync(int categoryId, CategoryRequest request)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(existingCategory => existingCategory.Id == categoryId);
            if (category is null)
            {
                return ServiceResult<CategoryResponse>.NotFound("Category was not found.");
            }

            if (await CategoryNameExistsAsync(request.Name, categoryId))
            {
                return ServiceResult<CategoryResponse>.Conflict("A category with this name already exists.");
            }

            category.Name = request.Name.Trim();
            category.Description = request.Description.Trim();
            category.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return ServiceResult<CategoryResponse>.Success(category.ToResponse());
        }

        public async Task<ServiceResult<IReadOnlyList<AchievementResponse>>> GetAchievementsAsync()
        {
            var achievements = await _context.Achievements
                .OrderBy(achievement => achievement.XPRequired)
                .ThenBy(achievement => achievement.Name)
                .ToListAsync();

            return ServiceResult<IReadOnlyList<AchievementResponse>>.Success(
                achievements.Select(achievement => achievement.ToResponse()).ToList());
        }

        public async Task<ServiceResult<AchievementResponse>> CreateAchievementAsync(AchievementRequest request)
        {
            if (await AchievementNameExistsAsync(request.Name, excludedAchievementId: null))
            {
                return ServiceResult<AchievementResponse>.Conflict("An achievement with this name already exists.");
            }

            var achievement = new Achievement
            {
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                XPRequired = request.XPRequired,
                BadgeImageUrl = request.BadgeImageUrl.Trim()
            };

            _context.Achievements.Add(achievement);
            await _context.SaveChangesAsync();

            return ServiceResult<AchievementResponse>.Created(achievement.ToResponse());
        }

        public async Task<ServiceResult<AchievementResponse>> UpdateAchievementAsync(int achievementId, AchievementRequest request)
        {
            var achievement = await _context.Achievements.FirstOrDefaultAsync(existingAchievement => existingAchievement.Id == achievementId);
            if (achievement is null)
            {
                return ServiceResult<AchievementResponse>.NotFound("Achievement was not found.");
            }

            if (await AchievementNameExistsAsync(request.Name, achievementId))
            {
                return ServiceResult<AchievementResponse>.Conflict("An achievement with this name already exists.");
            }

            achievement.Name = request.Name.Trim();
            achievement.Description = request.Description.Trim();
            achievement.XPRequired = request.XPRequired;
            achievement.BadgeImageUrl = request.BadgeImageUrl.Trim();

            await _context.SaveChangesAsync();
            return ServiceResult<AchievementResponse>.Success(achievement.ToResponse());
        }

        public async Task<ServiceResult<IReadOnlyList<AdminUserResponse>>> GetUsersAsync()
        {
            var users = await _context.Users
                .OrderBy(user => user.FullName)
                .Take(200)
                .ToListAsync();

            var response = new List<AdminUserResponse>();
            foreach (var user in users)
            {
                response.Add(new AdminUserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    IsActive = user.IsActive,
                    Roles = (await _userManager.GetRolesAsync(user)).ToList()
                });
            }

            return ServiceResult<IReadOnlyList<AdminUserResponse>>.Success(response);
        }

        public async Task<ServiceResult<AdminUserResponse>> UpdateUserStatusAsync(string userId, UpdateUserStatusRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == userId);
            if (user is null)
            {
                return ServiceResult<AdminUserResponse>.NotFound("User was not found.");
            }

            user.IsActive = request.IsActive;
            await _context.SaveChangesAsync();

            return ServiceResult<AdminUserResponse>.Success(new AdminUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                IsActive = user.IsActive,
                Roles = (await _userManager.GetRolesAsync(user)).ToList()
            });
        }

        public async Task<ServiceResult<bool>> ApproveWorkerVerificationAsync(int profileId, string adminUserId)
        {
            var profile = await _context.WorkerProfiles
                .Include(workerProfile => workerProfile.User)
                    .ThenInclude(user => user.Wallet)
                .Include(workerProfile => workerProfile.User)
                    .ThenInclude(user => user.UserXP)
                .FirstOrDefaultAsync(workerProfile => workerProfile.Id == profileId);

            if (profile is null)
            {
                return ServiceResult<bool>.NotFound("Worker verification request was not found.");
            }

            profile.VerificationStatus = VerificationStatus.Approved;
            profile.VerificationReviewedAt = DateTime.UtcNow;
            profile.VerificationReviewedByAdminId = adminUserId;
            profile.VerificationRejectionReason = null;
            profile.VerificationRejectionMessage = null;
            profile.User.IsActive = true;

            if (profile.User.Wallet is null)
            {
                _context.Wallets.Add(new Wallet { UserId = profile.UserId });
            }

            if (profile.User.UserXP is null)
            {
                _context.UserXPs.Add(new UserXP { UserId = profile.UserId });
            }

            await EnsureRoleAsync(profile.User, SideQuestRoles.Worker);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> RejectWorkerVerificationAsync(int profileId, string adminUserId, string reason, string? message)
        {
            var profile = await _context.WorkerProfiles
                .Include(workerProfile => workerProfile.User)
                .FirstOrDefaultAsync(workerProfile => workerProfile.Id == profileId);

            if (profile is null)
            {
                return ServiceResult<bool>.NotFound("Worker verification request was not found.");
            }

            profile.VerificationStatus = VerificationStatus.Rejected;
            profile.VerificationReviewedAt = DateTime.UtcNow;
            profile.VerificationReviewedByAdminId = adminUserId;
            profile.VerificationRejectionReason = reason.Trim();
            profile.VerificationRejectionMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();

            await RemoveRoleIfPresentAsync(profile.User, SideQuestRoles.Worker);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> ApproveCompanyVerificationAsync(int profileId, string adminUserId)
        {
            var profile = await _context.CompanyProfiles
                .Include(companyProfile => companyProfile.User)
                .FirstOrDefaultAsync(companyProfile => companyProfile.Id == profileId);

            if (profile is null)
            {
                return ServiceResult<bool>.NotFound("Company verification request was not found.");
            }

            profile.VerificationStatus = VerificationStatus.Approved;
            profile.VerificationReviewedAt = DateTime.UtcNow;
            profile.VerificationReviewedByAdminId = adminUserId;
            profile.VerificationRejectionReason = null;
            profile.VerificationRejectionMessage = null;
            profile.IsVerified = true;
            profile.VerifiedAt = DateTime.UtcNow;
            profile.User.IsActive = true;

            await EnsureRoleAsync(profile.User, SideQuestRoles.Employer);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> RejectCompanyVerificationAsync(int profileId, string adminUserId, string reason, string? message)
        {
            var profile = await _context.CompanyProfiles
                .Include(companyProfile => companyProfile.User)
                .FirstOrDefaultAsync(companyProfile => companyProfile.Id == profileId);

            if (profile is null)
            {
                return ServiceResult<bool>.NotFound("Company verification request was not found.");
            }

            profile.VerificationStatus = VerificationStatus.Rejected;
            profile.VerificationReviewedAt = DateTime.UtcNow;
            profile.VerificationReviewedByAdminId = adminUserId;
            profile.VerificationRejectionReason = reason.Trim();
            profile.VerificationRejectionMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
            profile.IsVerified = false;
            profile.VerifiedAt = null;

            await RemoveRoleIfPresentAsync(profile.User, SideQuestRoles.Employer);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        private Task<bool> CategoryNameExistsAsync(string name, int? excludedCategoryId)
        {
            var normalized = name.Trim().ToUpperInvariant();
            return _context.Categories.AnyAsync(category =>
                category.Name.ToUpper() == normalized &&
                (!excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value));
        }

        private Task<bool> AchievementNameExistsAsync(string name, int? excludedAchievementId)
        {
            var normalized = name.Trim().ToUpperInvariant();
            return _context.Achievements.AnyAsync(achievement =>
                achievement.Name.ToUpper() == normalized &&
                (!excludedAchievementId.HasValue || achievement.Id != excludedAchievementId.Value));
        }

        private async Task EnsureRoleAsync(ApplicationUser user, string role)
        {
            if (!await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        private async Task RemoveRoleIfPresentAsync(ApplicationUser user, string role)
        {
            if (await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.RemoveFromRoleAsync(user, role);
            }
        }

    }
}
