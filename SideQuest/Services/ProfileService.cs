using Microsoft.EntityFrameworkCore;
using SideQuest.Contracts;
using SideQuest.Data;
using SideQuest.Models;

namespace SideQuest.Services
{
    public interface IProfileService
    {
        Task<ServiceResult<WorkerProfileResponse>> GetWorkerProfileAsync(string userId);

        Task<ServiceResult<WorkerProfileResponse>> UpsertWorkerProfileAsync(string userId, UpsertWorkerProfileRequest request);

        Task<ServiceResult<WorkerProfileResponse>> SubmitWorkerVerificationAsync(string userId, SubmitWorkerVerificationRequest request);

        Task<ServiceResult<WorkerProfileResponse>> UpdateWorkerSkillsAsync(string userId, UpdateWorkerSkillsRequest request);

        Task<ServiceResult<CompanyProfileResponse>> GetCompanyProfileAsync(string userId);

        Task<ServiceResult<CompanyProfileResponse>> UpsertCompanyProfileAsync(string userId, UpsertCompanyProfileRequest request);

        Task<ServiceResult<CompanyProfileResponse>> SubmitCompanyVerificationAsync(string userId, SubmitCompanyVerificationRequest request);
    }

    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _context;

        public ProfileService(AppDbContext context)
        {
            _context = context;
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

            await _context.SaveChangesAsync();

            var savedProfile = await GetWorkerProfileQuery()
                .FirstAsync(workerProfile => workerProfile.UserId == userId);

            return ServiceResult<WorkerProfileResponse>.Success(savedProfile.ToResponse());
        }

        public async Task<ServiceResult<WorkerProfileResponse>> SubmitWorkerVerificationAsync(string userId, SubmitWorkerVerificationRequest request)
        {
            if (await _context.CompanyProfiles.AnyAsync(companyProfile => companyProfile.UserId == userId))
            {
                return ServiceResult<WorkerProfileResponse>.Conflict("This account is already onboarding as a company.");
            }

            var result = await UpsertWorkerProfileAsync(userId, request);
            if (!result.Succeeded)
            {
                return result;
            }

            var profile = await _context.WorkerProfiles
                .FirstAsync(workerProfile => workerProfile.UserId == userId);

            if (profile.VerificationStatus == VerificationStatus.Approved)
            {
                return ServiceResult<WorkerProfileResponse>.Conflict("This worker profile is already approved.");
            }

            profile.LegalName = request.LegalName.Trim();
            profile.NationalId = request.NationalId.Trim();
            profile.PhoneNumber = request.PhoneNumber.Trim();
            profile.ResidenceCountry = request.ResidenceCountry.Trim();
            profile.ResidenceCity = request.ResidenceCity.Trim();
            profile.VerificationDateOfBirth = request.VerificationDateOfBirth;
            profile.VerificationDocumentPath = string.IsNullOrWhiteSpace(request.VerificationDocumentPath) ? null : request.VerificationDocumentPath.Trim();
            profile.VerificationNotes = string.IsNullOrWhiteSpace(request.VerificationNotes) ? null : request.VerificationNotes.Trim();
            profile.VerificationStatus = VerificationStatus.Submitted;
            profile.VerificationSubmittedAt = DateTime.UtcNow;
            profile.VerificationReviewedAt = null;
            profile.VerificationReviewedByAdminId = null;
            profile.VerificationRejectionReason = null;
            profile.VerificationRejectionMessage = null;
            profile.UpdatedAt = DateTime.UtcNow;

            var user = await _context.Users.FirstAsync(applicationUser => applicationUser.Id == userId);
            user.FullName = request.LegalName.Trim();
            user.PhoneNumber = request.PhoneNumber.Trim();
            user.DateOfBirth = request.VerificationDateOfBirth;

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

            await _context.SaveChangesAsync();

            var savedProfile = await GetCompanyProfileQuery()
                .FirstAsync(companyProfile => companyProfile.UserId == userId);

            return ServiceResult<CompanyProfileResponse>.Success(savedProfile.ToResponse());
        }

        public async Task<ServiceResult<CompanyProfileResponse>> SubmitCompanyVerificationAsync(string userId, SubmitCompanyVerificationRequest request)
        {
            if (await _context.WorkerProfiles.AnyAsync(workerProfile => workerProfile.UserId == userId))
            {
                return ServiceResult<CompanyProfileResponse>.Conflict("This account is already onboarding as a worker.");
            }

            var result = await UpsertCompanyProfileAsync(userId, request);
            if (!result.Succeeded)
            {
                return result;
            }

            var profile = await _context.CompanyProfiles
                .FirstAsync(companyProfile => companyProfile.UserId == userId);

            if (profile.VerificationStatus == VerificationStatus.Approved)
            {
                return ServiceResult<CompanyProfileResponse>.Conflict("This company profile is already approved.");
            }

            profile.LegalCompanyName = request.LegalCompanyName.Trim();
            profile.RegistrationNumber = request.RegistrationNumber.Trim();
            profile.TaxNumber = string.IsNullOrWhiteSpace(request.TaxNumber) ? null : request.TaxNumber.Trim();
            profile.AuthorizedRepresentativeName = request.AuthorizedRepresentativeName.Trim();
            profile.AuthorizedRepresentativeNationalId = request.AuthorizedRepresentativeNationalId.Trim();
            profile.PhoneNumber = request.PhoneNumber.Trim();
            profile.Address = request.Address.Trim();
            profile.VerificationDocumentPath = string.IsNullOrWhiteSpace(request.VerificationDocumentPath) ? null : request.VerificationDocumentPath.Trim();
            profile.VerificationNotes = string.IsNullOrWhiteSpace(request.VerificationNotes) ? null : request.VerificationNotes.Trim();
            profile.VerificationStatus = VerificationStatus.Submitted;
            profile.VerificationSubmittedAt = DateTime.UtcNow;
            profile.VerificationReviewedAt = null;
            profile.VerificationReviewedByAdminId = null;
            profile.VerificationRejectionReason = null;
            profile.VerificationRejectionMessage = null;
            profile.IsVerified = false;
            profile.VerifiedAt = null;

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
            return _context.CompanyProfiles;
        }
    }
}
