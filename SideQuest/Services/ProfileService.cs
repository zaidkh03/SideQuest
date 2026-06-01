using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SideQuest.Authorization;
using SideQuest.Contracts;
using SideQuest.Data;
using SideQuest.Models;

namespace SideQuest.Services
{
    public interface IProfileService
    {
        Task<ServiceResult<WorkerProfileResponse>> GetWorkerProfileAsync(string userId);

        Task<ServiceResult<WorkerProfileResponse>> UpsertWorkerProfileAsync(string userId, UpsertWorkerProfileRequest request);

        Task<ServiceResult<WorkerProfileResponse>> UpdateWorkerSkillsAsync(string userId, UpdateWorkerSkillsRequest request);

        Task<ServiceResult<CompanyProfileResponse>> GetCompanyProfileAsync(string userId);

        Task<ServiceResult<CompanyProfileResponse>> UpsertCompanyProfileAsync(string userId, UpsertCompanyProfileRequest request);
    }

    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileService(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ServiceResult<WorkerProfileResponse>> GetWorkerProfileAsync(string userId)
        {
            var profile = await GetWorkerProfileQuery()
                .FirstOrDefaultAsync(workerProfile => workerProfile.UserId == userId);

            return profile is null
                ? ServiceResult<WorkerProfileResponse>.NotFound("Worker profile was not found.")
                : ServiceResult<WorkerProfileResponse>.Success(profile.ToResponse());
        }

        public async Task<ServiceResult<WorkerProfileResponse>> UpsertWorkerProfileAsync(string userId, UpsertWorkerProfileRequest request)
        {
            var user = await _context.Users
                .Include(applicationUser => applicationUser.Wallet)
                .Include(applicationUser => applicationUser.UserXP)
                .FirstOrDefaultAsync(applicationUser => applicationUser.Id == userId);

            if (user is null)
            {
                return ServiceResult<WorkerProfileResponse>.NotFound("User was not found.");
            }

            var profile = await _context.WorkerProfiles
                .FirstOrDefaultAsync(workerProfile => workerProfile.UserId == userId);

            if (profile is null)
            {
                profile = new WorkerProfile
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.WorkerProfiles.Add(profile);
            }

            profile.Headline = request.Headline.Trim();
            profile.Bio = request.Bio.Trim();
            profile.Location = request.Location.Trim();
            profile.HourlyRatePreference = request.HourlyRatePreference;
            profile.AvailabilityStatus = request.AvailabilityStatus;
            profile.PortfolioUrl = string.IsNullOrWhiteSpace(request.PortfolioUrl) ? null : request.PortfolioUrl.Trim();
            profile.ResumeUrl = string.IsNullOrWhiteSpace(request.ResumeUrl) ? null : request.ResumeUrl.Trim();
            profile.ExperienceYears = request.ExperienceYears;
            profile.UpdatedAt = DateTime.UtcNow;

            if (user.Wallet is null)
            {
                _context.Wallets.Add(new Wallet { UserId = userId });
            }

            if (user.UserXP is null)
            {
                _context.UserXPs.Add(new UserXP { UserId = userId });
            }

            await EnsureRoleAsync(user, SideQuestRoles.Worker);
            await _context.SaveChangesAsync();

            var savedProfile = await GetWorkerProfileQuery()
                .FirstAsync(workerProfile => workerProfile.UserId == userId);

            return ServiceResult<WorkerProfileResponse>.Success(savedProfile.ToResponse());
        }

        public async Task<ServiceResult<WorkerProfileResponse>> UpdateWorkerSkillsAsync(string userId, UpdateWorkerSkillsRequest request)
        {
            if (!await _context.WorkerProfiles.AnyAsync(workerProfile => workerProfile.UserId == userId))
            {
                return ServiceResult<WorkerProfileResponse>.NotFound("Worker profile was not found.");
            }

            var duplicateSkillIds = request.Skills
                .GroupBy(skill => skill.SkillId)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicateSkillIds.Count > 0)
            {
                return ServiceResult<WorkerProfileResponse>.Validation(
                    nameof(request.Skills),
                    $"Duplicate skill ids are not allowed: {string.Join(", ", duplicateSkillIds)}.");
            }

            var requestedSkillIds = request.Skills.Select(skill => skill.SkillId).ToList();
            var existingSkillIds = await _context.Skills
                .Where(skill => requestedSkillIds.Contains(skill.Id))
                .Select(skill => skill.Id)
                .ToListAsync();

            var missingSkillIds = requestedSkillIds.Except(existingSkillIds).ToList();
            if (missingSkillIds.Count > 0)
            {
                return ServiceResult<WorkerProfileResponse>.Validation(
                    nameof(request.Skills),
                    $"Unknown skill ids: {string.Join(", ", missingSkillIds)}.");
            }

            var existingUserSkills = await _context.UserSkills
                .Where(userSkill => userSkill.UserId == userId)
                .ToListAsync();

            _context.UserSkills.RemoveRange(existingUserSkills);

            _context.UserSkills.AddRange(request.Skills.Select(skill => new UserSkill
            {
                UserId = userId,
                SkillId = skill.SkillId,
                SkillLevel = skill.SkillLevel
            }));

            await _context.SaveChangesAsync();

            var profile = await GetWorkerProfileQuery()
                .FirstAsync(workerProfile => workerProfile.UserId == userId);

            return ServiceResult<WorkerProfileResponse>.Success(profile.ToResponse());
        }

        public async Task<ServiceResult<CompanyProfileResponse>> GetCompanyProfileAsync(string userId)
        {
            var profile = await GetCompanyProfileQuery()
                .FirstOrDefaultAsync(companyProfile => companyProfile.UserId == userId);

            return profile is null
                ? ServiceResult<CompanyProfileResponse>.NotFound("Company profile was not found.")
                : ServiceResult<CompanyProfileResponse>.Success(profile.ToResponse());
        }

        public async Task<ServiceResult<CompanyProfileResponse>> UpsertCompanyProfileAsync(string userId, UpsertCompanyProfileRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(applicationUser => applicationUser.Id == userId);
            if (user is null)
            {
                return ServiceResult<CompanyProfileResponse>.NotFound("User was not found.");
            }

            var profile = await _context.CompanyProfiles
                .Include(companyProfile => companyProfile.CompanySubscriptions)
                .FirstOrDefaultAsync(companyProfile => companyProfile.UserId == userId);

            if (profile is null)
            {
                profile = new CompanyProfile
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CompanyProfiles.Add(profile);
            }

            profile.CompanyName = request.CompanyName.Trim();
            profile.Description = request.Description.Trim();
            profile.Location = request.Location.Trim();
            profile.Website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim();
            profile.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();

            await EnsureFreeSubscriptionAsync(profile);
            await EnsureRoleAsync(user, SideQuestRoles.Employer);
            await _context.SaveChangesAsync();

            var savedProfile = await GetCompanyProfileQuery()
                .FirstAsync(companyProfile => companyProfile.UserId == userId);

            return ServiceResult<CompanyProfileResponse>.Success(savedProfile.ToResponse());
        }

        private IQueryable<WorkerProfile> GetWorkerProfileQuery()
        {
            return _context.WorkerProfiles
                .Include(workerProfile => workerProfile.User)
                    .ThenInclude(user => user.UserSkills)
                    .ThenInclude(userSkill => userSkill.Skill);
        }

        private IQueryable<CompanyProfile> GetCompanyProfileQuery()
        {
            return _context.CompanyProfiles
                .Include(companyProfile => companyProfile.CompanySubscriptions)
                    .ThenInclude(subscription => subscription.Plan);
        }

        private async Task EnsureRoleAsync(ApplicationUser user, string role)
        {
            if (!await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        private async Task EnsureFreeSubscriptionAsync(CompanyProfile profile)
        {
            if (profile.CompanySubscriptions.Any(subscription => subscription.IsActive))
            {
                return;
            }

            var freePlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(plan => plan.Name == "Free");

            if (freePlan is null)
            {
                return;
            }

            profile.CompanySubscriptions.Add(new CompanySubscription
            {
                Plan = freePlan,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                IsActive = true
            });
        }
    }
}
