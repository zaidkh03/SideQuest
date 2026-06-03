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
    public class OnboardingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IProfileService _profileService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OnboardingController(
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
            var roleRedirect = RedirectToRoleDashboard();
            if (roleRedirect is not null)
            {
                return roleRedirect;
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (await HasAnyProfileAsync(userId))
            {
                return RedirectToAction(nameof(Status));
            }

            return RedirectToAction(nameof(Select));
        }

        public async Task<IActionResult> Select()
        {
            var roleRedirect = RedirectToRoleDashboard();
            if (roleRedirect is not null)
            {
                return roleRedirect;
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (await HasAnyProfileAsync(userId))
            {
                return RedirectToAction(nameof(Status));
            }

            SetOnboardingViewData("Choose Account Type");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Worker()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (await _context.CompanyProfiles.AnyAsync(companyProfile => companyProfile.UserId == userId))
            {
                return RedirectToAction(nameof(Status));
            }

            var user = await _context.Users
                .Include(applicationUser => applicationUser.WorkerProfile)
                .FirstOrDefaultAsync(applicationUser => applicationUser.Id == userId);

            if (user is null)
            {
                return Challenge();
            }

            var profile = user.WorkerProfile;
            if (profile is null)
            {
                profile = new WorkerProfile
                {
                    UserId = userId,
                    LegalName = user.FullName,
                    Headline = string.Empty,
                    Bio = string.Empty,
                    Location = string.Empty,
                    VerificationDateOfBirth = user.DateOfBirth,
                    VerificationStatus = VerificationStatus.Draft,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.WorkerProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            if (profile.VerificationStatus != VerificationStatus.Draft)
            {
                return RedirectToAction(nameof(Status));
            }

            SetOnboardingViewData("Worker Verification");
            return View(ToWorkerForm(profile, user));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Worker(WorkerVerificationFormViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                SetOnboardingViewData("Worker Verification");
                return View(model);
            }

            var currentStatus = await _context.WorkerProfiles
                .Where(workerProfile => workerProfile.UserId == userId)
                .Select(workerProfile => workerProfile.VerificationStatus)
                .FirstOrDefaultAsync();

            if (currentStatus is VerificationStatus.Submitted or VerificationStatus.Approved or VerificationStatus.Rejected)
            {
                return RedirectToAction(nameof(Status));
            }

            var result = await _profileService.SubmitWorkerVerificationAsync(userId, new SubmitWorkerVerificationRequest
            {
                Headline = model.Headline,
                Bio = model.Bio,
                Location = model.Location,
                HourlyRatePreference = model.HourlyRatePreference,
                AvailabilityStatus = model.AvailabilityStatus,
                PortfolioUrl = model.PortfolioUrl,
                ResumeUrl = model.ResumeUrl,
                ExperienceYears = model.ExperienceYears,
                LegalName = model.LegalName,
                NationalId = model.NationalId,
                PhoneNumber = model.PhoneNumber,
                ResidenceCountry = model.ResidenceCountry,
                ResidenceCity = model.ResidenceCity,
                VerificationDateOfBirth = model.VerificationDateOfBirth,
                VerificationDocumentPath = model.VerificationDocumentPath,
                VerificationNotes = model.VerificationNotes
            });

            if (!result.Succeeded)
            {
                AddServiceErrors(result.Errors, result.Message);
                SetOnboardingViewData("Worker Verification");
                return View(model);
            }

            return RedirectToAction(nameof(Status));
        }

        [HttpGet]
        public async Task<IActionResult> Company()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (await _context.WorkerProfiles.AnyAsync(workerProfile => workerProfile.UserId == userId))
            {
                return RedirectToAction(nameof(Status));
            }

            var user = await _context.Users
                .Include(applicationUser => applicationUser.CompanyProfile)
                .FirstOrDefaultAsync(applicationUser => applicationUser.Id == userId);

            if (user is null)
            {
                return Challenge();
            }

            var profile = user.CompanyProfile;
            if (profile is null)
            {
                profile = new CompanyProfile
                {
                    UserId = userId,
                    CompanyName = string.IsNullOrWhiteSpace(user.FullName) ? "Company" : user.FullName,
                    LegalCompanyName = string.IsNullOrWhiteSpace(user.FullName) ? null : user.FullName,
                    AuthorizedRepresentativeName = user.FullName,
                    Description = string.Empty,
                    Location = string.Empty,
                    VerificationStatus = VerificationStatus.Draft,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CompanyProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            if (profile.VerificationStatus != VerificationStatus.Draft)
            {
                return RedirectToAction(nameof(Status));
            }

            SetOnboardingViewData("Company Verification");
            return View(ToCompanyForm(profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Company(CompanyVerificationFormViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                SetOnboardingViewData("Company Verification");
                return View(model);
            }

            var currentStatus = await _context.CompanyProfiles
                .Where(companyProfile => companyProfile.UserId == userId)
                .Select(companyProfile => companyProfile.VerificationStatus)
                .FirstOrDefaultAsync();

            if (currentStatus is VerificationStatus.Submitted or VerificationStatus.Approved or VerificationStatus.Rejected)
            {
                return RedirectToAction(nameof(Status));
            }

            var result = await _profileService.SubmitCompanyVerificationAsync(userId, new SubmitCompanyVerificationRequest
            {
                CompanyName = model.CompanyName,
                Description = model.Description,
                Location = model.Location,
                Website = model.Website,
                LogoUrl = model.LogoUrl,
                LegalCompanyName = model.LegalCompanyName,
                RegistrationNumber = model.RegistrationNumber,
                TaxNumber = model.TaxNumber,
                AuthorizedRepresentativeName = model.AuthorizedRepresentativeName,
                AuthorizedRepresentativeNationalId = model.AuthorizedRepresentativeNationalId,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                VerificationDocumentPath = model.VerificationDocumentPath,
                VerificationNotes = model.VerificationNotes
            });

            if (!result.Succeeded)
            {
                AddServiceErrors(result.Errors, result.Message);
                SetOnboardingViewData("Company Verification");
                return View(model);
            }

            return RedirectToAction(nameof(Status));
        }

        public async Task<IActionResult> Status()
        {
            var roleRedirect = RedirectToRoleDashboard();
            if (roleRedirect is not null)
            {
                return roleRedirect;
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var workerProfile = await _context.WorkerProfiles
                .AsNoTracking()
                .Include(workerProfile => workerProfile.User)
                .FirstOrDefaultAsync(workerProfile => workerProfile.UserId == userId);

            if (workerProfile is not null)
            {
                SetOnboardingViewData("Worker Status");
                return View(new OnboardingStatusViewModel
                {
                    AccountType = OnboardingAccountType.Worker,
                    Status = workerProfile.VerificationStatus,
                    DisplayName = string.IsNullOrWhiteSpace(workerProfile.LegalName) ? workerProfile.User.FullName : workerProfile.LegalName,
                    RejectionReason = workerProfile.VerificationRejectionReason,
                    RejectionMessage = workerProfile.VerificationRejectionMessage,
                    SubmittedAt = workerProfile.VerificationSubmittedAt,
                    ReviewedAt = workerProfile.VerificationReviewedAt,
                    ContinueAction = workerProfile.VerificationStatus == VerificationStatus.Draft ? nameof(Worker) : null
                });
            }

            var companyProfile = await _context.CompanyProfiles
                .AsNoTracking()
                .Include(companyProfile => companyProfile.User)
                .FirstOrDefaultAsync(companyProfile => companyProfile.UserId == userId);

            if (companyProfile is not null)
            {
                SetOnboardingViewData("Company Status");
                return View(new OnboardingStatusViewModel
                {
                    AccountType = OnboardingAccountType.Company,
                    Status = companyProfile.VerificationStatus,
                    DisplayName = string.IsNullOrWhiteSpace(companyProfile.CompanyName) ? companyProfile.User.FullName : companyProfile.CompanyName,
                    RejectionReason = companyProfile.VerificationRejectionReason,
                    RejectionMessage = companyProfile.VerificationRejectionMessage,
                    SubmittedAt = companyProfile.VerificationSubmittedAt,
                    ReviewedAt = companyProfile.VerificationReviewedAt,
                    ContinueAction = companyProfile.VerificationStatus == VerificationStatus.Draft ? nameof(Company) : null
                });
            }

            return RedirectToAction(nameof(Select));
        }

        private RedirectToActionResult? RedirectToRoleDashboard()
        {
            if (User.IsInRole(SideQuestRoles.Admin))
            {
                return RedirectToAction("Admin", "Dashboard");
            }

            if (User.IsInRole(SideQuestRoles.Employer))
            {
                return RedirectToAction("Employer", "Dashboard");
            }

            if (User.IsInRole(SideQuestRoles.Worker))
            {
                return RedirectToAction("Worker", "Dashboard");
            }

            return null;
        }

        private async Task<bool> HasAnyProfileAsync(string userId)
        {
            if (await _context.WorkerProfiles.AnyAsync(workerProfile => workerProfile.UserId == userId))
            {
                return true;
            }

            return await _context.CompanyProfiles.AnyAsync(companyProfile => companyProfile.UserId == userId);
        }

        private void SetOnboardingViewData(string title)
        {
            ViewData["Title"] = title;
            ViewData["TopBarTitle"] = "Account Review";
            ViewData["Shell"] = "App";
            ViewData["ActivePortal"] = "Onboarding";
            ViewData["ActiveNav"] = "Verification";
        }

        private void AddServiceErrors(IReadOnlyDictionary<string, string[]> errors, string? fallbackMessage)
        {
            if (errors.Count == 0)
            {
                ModelState.AddModelError(string.Empty, fallbackMessage ?? "We could not save this verification request.");
                return;
            }

            foreach (var error in errors)
            {
                foreach (var message in error.Value)
                {
                    ModelState.AddModelError(error.Key, message);
                }
            }
        }

        private static WorkerVerificationFormViewModel ToWorkerForm(WorkerProfile profile, ApplicationUser user)
        {
            return new WorkerVerificationFormViewModel
            {
                Headline = profile.Headline,
                Bio = profile.Bio,
                Location = profile.Location,
                HourlyRatePreference = profile.HourlyRatePreference,
                AvailabilityStatus = profile.AvailabilityStatus,
                PortfolioUrl = profile.PortfolioUrl,
                ResumeUrl = profile.ResumeUrl,
                ExperienceYears = profile.ExperienceYears,
                LegalName = string.IsNullOrWhiteSpace(profile.LegalName) ? user.FullName : profile.LegalName,
                NationalId = profile.NationalId ?? string.Empty,
                PhoneNumber = profile.PhoneNumber ?? user.PhoneNumber ?? string.Empty,
                ResidenceCountry = profile.ResidenceCountry ?? string.Empty,
                ResidenceCity = profile.ResidenceCity ?? string.Empty,
                VerificationDateOfBirth = profile.VerificationDateOfBirth ?? user.DateOfBirth,
                VerificationDocumentPath = profile.VerificationDocumentPath,
                VerificationNotes = profile.VerificationNotes
            };
        }

        private static CompanyVerificationFormViewModel ToCompanyForm(CompanyProfile profile)
        {
            return new CompanyVerificationFormViewModel
            {
                CompanyName = profile.CompanyName,
                Description = profile.Description,
                Location = profile.Location,
                Website = profile.Website,
                LogoUrl = profile.LogoUrl,
                LegalCompanyName = profile.LegalCompanyName ?? profile.CompanyName,
                RegistrationNumber = profile.RegistrationNumber ?? string.Empty,
                TaxNumber = profile.TaxNumber,
                AuthorizedRepresentativeName = profile.AuthorizedRepresentativeName ?? string.Empty,
                AuthorizedRepresentativeNationalId = profile.AuthorizedRepresentativeNationalId ?? string.Empty,
                PhoneNumber = profile.PhoneNumber ?? string.Empty,
                Address = profile.Address ?? string.Empty,
                VerificationDocumentPath = profile.VerificationDocumentPath,
                VerificationNotes = profile.VerificationNotes
            };
        }
    }
}
