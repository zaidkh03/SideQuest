using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuest.Authorization;
using SideQuest.Contracts;
using SideQuest.Data;
using SideQuest.Models;
using SideQuest.Services;
using SideQuest.ViewModels;

namespace SideQuest.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IProfileService _profileService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(
            AppDbContext context,
            IProfileService profileService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _profileService = profileService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            SetProfileViewData("Profile", "Profile");

            var user = await GetCurrentUserWithProfileAsync();
            if (user is null)
            {
                return Challenge();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var displayName = PortalPageMapping.DisplayName(user);
            var stats = await BuildProfileStatsAsync(user, roles);

            var model = new ProfileOverviewViewModel
            {
                DisplayName = displayName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Initials = PortalPageMapping.Initials(displayName),
                ProfileImageUrl = user.ProfileImageUrl,
                RoleLabel = roles.FirstOrDefault() ?? "Pending",
                VerificationStatus = user.WorkerProfile?.VerificationStatus ?? user.CompanyProfile?.VerificationStatus,
                Headline = user.WorkerProfile?.Headline ?? user.CompanyProfile?.CompanyName ?? "SideQuest member",
                Bio = user.WorkerProfile?.Bio ?? user.CompanyProfile?.Description ?? "Complete your profile to make this page useful.",
                Location = user.WorkerProfile?.Location ?? user.CompanyProfile?.Location ?? string.Empty,
                Stats = stats,
                Skills = user.UserSkills
                    .OrderBy(userSkill => userSkill.Skill.Name)
                    .Select(userSkill => new UserSkillResponse
                    {
                        SkillId = userSkill.SkillId,
                        Name = userSkill.Skill.Name,
                        SkillLevel = userSkill.SkillLevel
                    })
                    .ToList(),
                Achievements = user.UserAchievements
                    .OrderByDescending(userAchievement => userAchievement.EarnedAt)
                    .Select(userAchievement => userAchievement.Achievement.Name)
                    .ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Edit()
        {
            SetProfileViewData("Profile", "Edit Profile");

            var user = await GetCurrentUserWithProfileAsync();
            if (user is null)
            {
                return Challenge();
            }

            var roles = await _userManager.GetRolesAsync(user);
            return View(ToEditModel(user, roles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
            SetProfileViewData("Profile", "Edit Profile");

            var user = await GetCurrentUserWithProfileAsync();
            if (user is null)
            {
                return Challenge();
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(SideQuestRoles.Worker))
            {
                RemoveModelStatePrefix("Company.");
                ValidateWorkerProfileSection(model.Worker);
            }
            else if (roles.Contains(SideQuestRoles.Employer))
            {
                RemoveModelStatePrefix("Worker.");
            }
            else
            {
                RemoveModelStatePrefix("Worker.");
                RemoveModelStatePrefix("Company.");
            }

            if (!ModelState.IsValid)
            {
                model.RoleLabel = roles.FirstOrDefault() ?? "Pending";
                return View(model);
            }

            user.FullName = model.FullName.Trim();
            user.ProfileImageUrl = string.IsNullOrWhiteSpace(model.ProfileImageUrl) ? null : model.ProfileImageUrl.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
            user.DateOfBirth = model.DateOfBirth;
            await _userManager.UpdateAsync(user);

            if (roles.Contains(SideQuestRoles.Worker))
            {
                var result = await _profileService.UpsertWorkerProfileAsync(user.Id, new UpsertWorkerProfileRequest
                {
                    Headline = model.Worker.Headline,
                    Bio = model.Worker.Bio,
                    Location = model.Worker.Location,
                    HourlyRatePreference = model.Worker.HourlyRatePreference,
                    AvailabilityStatus = model.Worker.AvailabilityStatus,
                    PortfolioUrl = model.Worker.PortfolioUrl,
                    ResumeUrl = model.Worker.ResumeUrl,
                    ExperienceYears = model.Worker.ExperienceYears
                });

                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = result.Message ?? "We could not update your worker profile.";
                    return RedirectToAction(nameof(Edit));
                }
            }

            if (roles.Contains(SideQuestRoles.Employer))
            {
                var result = await _profileService.UpsertCompanyProfileAsync(user.Id, new UpsertCompanyProfileRequest
                {
                    CompanyName = model.Company.CompanyName,
                    Description = model.Company.Description,
                    Location = model.Company.Location,
                    Website = model.Company.Website,
                    LogoUrl = model.Company.LogoUrl
                });

                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = result.Message ?? "We could not update your company profile.";
                    return RedirectToAction(nameof(Edit));
                }
            }

            TempData["SuccessMessage"] = "Profile updated.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = SideQuestRoles.Worker)]
        public async Task<IActionResult> Skills()
        {
            SetProfileViewData("Profile", "Skills");

            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var selected = await _context.UserSkills
                .AsNoTracking()
                .Where(userSkill => userSkill.UserId == userId)
                .ToDictionaryAsync(userSkill => userSkill.SkillId, userSkill => userSkill.SkillLevel);

            var skills = await _context.Skills
                .AsNoTracking()
                .OrderBy(skill => skill.Name)
                .Select(skill => new SkillSelectionViewModel
                {
                    SkillId = skill.Id,
                    Name = skill.Name,
                    IsSelected = selected.ContainsKey(skill.Id),
                    SkillLevel = selected.ContainsKey(skill.Id) ? selected[skill.Id] : 1
                })
                .ToListAsync();

            return View(new WorkerSkillsPageViewModel { Skills = skills });
        }

        [HttpPost]
        [Authorize(Roles = SideQuestRoles.Worker)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Skills(UpdateSkillsFormViewModel form)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var result = await _profileService.UpdateWorkerSkillsAsync(userId, new UpdateWorkerSkillsRequest
            {
                Skills = form.SelectedSkillIds
                    .Distinct()
                    .Select(skillId => new UserSkillRequest
                    {
                        SkillId = skillId,
                        SkillLevel = Math.Clamp(form.SkillLevels.TryGetValue(skillId, out var level) ? level : 1, 1, 5)
                    })
                    .ToList()
            });

            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "Skills updated."
                : result.Message ?? "We could not update your skills.";

            return RedirectToAction(nameof(Skills));
        }

        private async Task<ApplicationUser?> GetCurrentUserWithProfileAsync()
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return null;
            }

            return await _context.Users
                .Include(user => user.WorkerProfile)
                .Include(user => user.CompanyProfile)
                .Include(user => user.Wallet)
                .Include(user => user.UserXP)
                .Include(user => user.UserSkills)
                    .ThenInclude(userSkill => userSkill.Skill)
                .Include(user => user.UserAchievements)
                    .ThenInclude(userAchievement => userAchievement.Achievement)
                .FirstOrDefaultAsync(user => user.Id == userId);
        }

        private async Task<IReadOnlyList<StatCardViewModel>> BuildProfileStatsAsync(ApplicationUser user, IList<string> roles)
        {
            if (roles.Contains(SideQuestRoles.Worker))
            {
                return
                [
                    new StatCardViewModel { Label = "Completed", Value = (user.WorkerProfile?.TotalJobsCompleted ?? 0).ToString(), Detail = "Finished quests", Icon = "task_alt", Accent = "secondary" },
                    new StatCardViewModel { Label = "Rating", Value = $"{user.WorkerProfile?.AverageRating ?? 0:0.0}", Detail = "Average review", Icon = "star", Accent = "tertiary" },
                    new StatCardViewModel { Label = "Wallet", Value = $"{user.Wallet?.CurrentBalance ?? 0:C}", Detail = $"{user.Wallet?.TotalEarned ?? 0:C} earned", Icon = "account_balance_wallet", Accent = "primary" },
                    new StatCardViewModel { Label = "Level", Value = (user.UserXP?.Level ?? 1).ToString(), Detail = $"{user.UserXP?.XP ?? 0} XP", Icon = "workspace_premium", Accent = "secondary" }
                ];
            }

            if (roles.Contains(SideQuestRoles.Employer) && user.CompanyProfile is not null)
            {
                var activeJobs = await _context.Jobs.CountAsync(job => job.CompanyId == user.CompanyProfile.Id && job.Status != JobStatus.Completed && job.Status != JobStatus.Cancelled);
                var applications = await _context.JobApplications.CountAsync(application => application.Job.CompanyId == user.CompanyProfile.Id);

                return
                [
                    new StatCardViewModel { Label = "Active Jobs", Value = activeJobs.ToString(), Detail = "Current pipeline", Icon = "work", Accent = "primary" },
                    new StatCardViewModel { Label = "Applications", Value = applications.ToString(), Detail = "Across company jobs", Icon = "group", Accent = "secondary" },
                    new StatCardViewModel { Label = "Verification", Value = user.CompanyProfile.VerificationStatus.ToString(), Detail = user.CompanyProfile.IsVerified ? "Verified company" : "Verification pending", Icon = "business_center", Accent = "tertiary" }
                ];
            }

            if (roles.Contains(SideQuestRoles.Admin))
            {
                return [];
            }

            return [];
        }

        private static ProfileEditViewModel ToEditModel(ApplicationUser user, IList<string> roles)
        {
            return new ProfileEditViewModel
            {
                FullName = user.FullName,
                ProfileImageUrl = user.ProfileImageUrl,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                RoleLabel = roles.FirstOrDefault() ?? "Pending",
                Worker = new WorkerProfileEditSectionViewModel
                {
                    Headline = user.WorkerProfile?.Headline ?? string.Empty,
                    Bio = user.WorkerProfile?.Bio ?? string.Empty,
                    Location = user.WorkerProfile?.Location ?? string.Empty,
                    HourlyRatePreference = user.WorkerProfile?.HourlyRatePreference,
                    AvailabilityStatus = user.WorkerProfile?.AvailabilityStatus ?? AvailabilityStatus.Available,
                    PortfolioUrl = user.WorkerProfile?.PortfolioUrl,
                    ResumeUrl = user.WorkerProfile?.ResumeUrl,
                    ExperienceYears = user.WorkerProfile?.ExperienceYears ?? 0
                },
                Company = new CompanyProfileFormViewModel
                {
                    CompanyName = user.CompanyProfile?.CompanyName ?? string.Empty,
                    Description = user.CompanyProfile?.Description ?? string.Empty,
                    Location = user.CompanyProfile?.Location ?? string.Empty,
                    Website = user.CompanyProfile?.Website,
                    LogoUrl = user.CompanyProfile?.LogoUrl
                }
            };
        }

        private void ValidateWorkerProfileSection(WorkerProfileEditSectionViewModel worker)
        {
            if (string.IsNullOrWhiteSpace(worker.Headline))
            {
                ModelState.AddModelError("Worker.Headline", "Headline is required.");
            }

            if (string.IsNullOrWhiteSpace(worker.Bio))
            {
                ModelState.AddModelError("Worker.Bio", "Bio is required.");
            }

            if (string.IsNullOrWhiteSpace(worker.Location))
            {
                ModelState.AddModelError("Worker.Location", "Location is required.");
            }
        }

        private void RemoveModelStatePrefix(string prefix)
        {
            foreach (var key in ModelState.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                ModelState.Remove(key);
            }
        }

        private string? GetUserId()
        {
            return _userManager.GetUserId(User);
        }

        private void SetProfileViewData(string activeNav, string title)
        {
            ViewData["Title"] = title;
            ViewData["TopBarTitle"] = "Profile";
            ViewData["Shell"] = "App";
            ViewData["ActivePortal"] = User.IsInRole(SideQuestRoles.Admin)
                ? "Admin"
                : User.IsInRole(SideQuestRoles.Employer)
                    ? "Employer"
                    : User.IsInRole(SideQuestRoles.Worker)
                        ? "Worker"
                        : "Onboarding";
            ViewData["ActiveNav"] = activeNav;
        }
    }
}
