using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuest.Authorization;
using SideQuest.Data;
using SideQuest.Models;
using SideQuest.Services;
using SideQuest.ViewModels;

namespace SideQuest.Controllers
{
    [Authorize(Roles = SideQuestRoles.Admin)]
    [Route("Admin/Verifications")]
    public class AdminVerificationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAdminService _adminService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminVerificationController(
            AppDbContext context,
            IAdminService adminService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _adminService = adminService;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            SetAdminViewData("Verification Queue");

            var workers = await _context.WorkerProfiles
                .AsNoTracking()
                .Include(workerProfile => workerProfile.User)
                .Where(workerProfile => workerProfile.VerificationStatus == VerificationStatus.Submitted)
                .OrderBy(workerProfile => workerProfile.VerificationSubmittedAt)
                .Take(50)
                .Select(workerProfile => new WorkerVerificationQueueItemViewModel
                {
                    ProfileId = workerProfile.Id,
                    UserName = workerProfile.User.FullName,
                    Email = workerProfile.User.Email ?? string.Empty,
                    Headline = workerProfile.Headline,
                    Location = workerProfile.Location,
                    MaskedNationalId = MaskIdentifier(workerProfile.NationalId),
                    SubmittedAt = workerProfile.VerificationSubmittedAt
                })
                .ToListAsync();

            var companies = await _context.CompanyProfiles
                .AsNoTracking()
                .Include(companyProfile => companyProfile.User)
                .Where(companyProfile => companyProfile.VerificationStatus == VerificationStatus.Submitted)
                .OrderBy(companyProfile => companyProfile.VerificationSubmittedAt)
                .Take(50)
                .Select(companyProfile => new CompanyVerificationQueueItemViewModel
                {
                    ProfileId = companyProfile.Id,
                    UserName = companyProfile.User.FullName,
                    Email = companyProfile.User.Email ?? string.Empty,
                    CompanyName = companyProfile.CompanyName,
                    RegistrationNumber = MaskIdentifier(companyProfile.RegistrationNumber),
                    Location = companyProfile.Location,
                    SubmittedAt = companyProfile.VerificationSubmittedAt
                })
                .ToListAsync();

            var model = new VerificationQueueViewModel
            {
                SubmittedWorkers = workers.Count,
                SubmittedCompanies = companies.Count,
                RejectedRequests = await CountProfilesAsync(VerificationStatus.Rejected),
                ApprovedRequests = await CountProfilesAsync(VerificationStatus.Approved),
                Workers = workers,
                Companies = companies
            };

            return View(model);
        }

        [HttpGet("Worker/{id:int}")]
        public async Task<IActionResult> Worker(int id)
        {
            SetAdminViewData("Worker Verification");

            var profile = await _context.WorkerProfiles
                .AsNoTracking()
                .Include(workerProfile => workerProfile.User)
                .FirstOrDefaultAsync(workerProfile => workerProfile.Id == id);

            if (profile is null)
            {
                return NotFound();
            }

            return View(new WorkerVerificationReviewViewModel
            {
                ProfileId = profile.Id,
                UserName = profile.User.FullName,
                Email = profile.User.Email ?? string.Empty,
                Headline = profile.Headline,
                Bio = profile.Bio,
                Location = profile.Location,
                LegalName = profile.LegalName ?? profile.User.FullName,
                MaskedNationalId = MaskIdentifier(profile.NationalId),
                PhoneNumber = profile.PhoneNumber ?? string.Empty,
                Residence = string.Join(", ", new[] { profile.ResidenceCity, profile.ResidenceCountry }.Where(value => !string.IsNullOrWhiteSpace(value))),
                DateOfBirth = profile.VerificationDateOfBirth,
                VerificationDocumentPath = profile.VerificationDocumentPath,
                VerificationNotes = profile.VerificationNotes,
                Status = profile.VerificationStatus,
                SubmittedAt = profile.VerificationSubmittedAt
            });
        }

        [HttpGet("Company/{id:int}")]
        public async Task<IActionResult> Company(int id)
        {
            SetAdminViewData("Company Verification");

            var profile = await _context.CompanyProfiles
                .AsNoTracking()
                .Include(companyProfile => companyProfile.User)
                .FirstOrDefaultAsync(companyProfile => companyProfile.Id == id);

            if (profile is null)
            {
                return NotFound();
            }

            return View(new CompanyVerificationReviewViewModel
            {
                ProfileId = profile.Id,
                UserName = profile.User.FullName,
                Email = profile.User.Email ?? string.Empty,
                CompanyName = profile.CompanyName,
                LegalCompanyName = profile.LegalCompanyName ?? profile.CompanyName,
                Description = profile.Description,
                Location = profile.Location,
                RegistrationNumber = MaskIdentifier(profile.RegistrationNumber),
                TaxNumber = string.IsNullOrWhiteSpace(profile.TaxNumber) ? null : MaskIdentifier(profile.TaxNumber),
                AuthorizedRepresentativeName = profile.AuthorizedRepresentativeName ?? string.Empty,
                MaskedRepresentativeNationalId = MaskIdentifier(profile.AuthorizedRepresentativeNationalId),
                PhoneNumber = profile.PhoneNumber ?? string.Empty,
                Address = profile.Address ?? string.Empty,
                Website = profile.Website,
                VerificationDocumentPath = profile.VerificationDocumentPath,
                VerificationNotes = profile.VerificationNotes,
                Status = profile.VerificationStatus,
                SubmittedAt = profile.VerificationSubmittedAt
            });
        }

        [HttpPost("Worker/{id:int}/Approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveWorker(int id)
        {
            var adminId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Challenge();
            }

            var result = await _adminService.ApproveWorkerVerificationAsync(id, adminId);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "Worker verification approved. Portal access is now active."
                : result.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Worker/{id:int}/Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectWorker(int id, VerificationDecisionViewModel decision)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Choose a rejection reason before sending the decision.";
                return RedirectToAction(nameof(Worker), new { id });
            }

            var adminId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Challenge();
            }

            var result = await _adminService.RejectWorkerVerificationAsync(id, adminId, decision.RejectionReason, decision.RejectionMessage);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "Worker verification rejected and the user was notified through their status page."
                : result.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Company/{id:int}/Approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCompany(int id)
        {
            var adminId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Challenge();
            }

            var result = await _adminService.ApproveCompanyVerificationAsync(id, adminId);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "Company verification approved. Employer portal access is now active."
                : result.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Company/{id:int}/Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCompany(int id, VerificationDecisionViewModel decision)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Choose a rejection reason before sending the decision.";
                return RedirectToAction(nameof(Company), new { id });
            }

            var adminId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Challenge();
            }

            var result = await _adminService.RejectCompanyVerificationAsync(id, adminId, decision.RejectionReason, decision.RejectionMessage);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "Company verification rejected and the user was notified through their status page."
                : result.Message;

            return RedirectToAction(nameof(Index));
        }

        private void SetAdminViewData(string title)
        {
            ViewData["Title"] = title;
            ViewData["TopBarTitle"] = "Command Center";
            ViewData["Shell"] = "App";
            ViewData["ActivePortal"] = "Admin";
            ViewData["ActiveNav"] = "Verifications";
        }

        private async Task<int> CountProfilesAsync(VerificationStatus status)
        {
            var workerCount = await _context.WorkerProfiles.CountAsync(workerProfile => workerProfile.VerificationStatus == status);
            var companyCount = await _context.CompanyProfiles.CountAsync(companyProfile => companyProfile.VerificationStatus == status);
            return workerCount + companyCount;
        }

        private static string MaskIdentifier(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Not provided";
            }

            var trimmed = value.Trim();
            if (trimmed.Length <= 4)
            {
                return "****";
            }

            return $"{new string('*', trimmed.Length - 4)}{trimmed[^4..]}";
        }
    }
}
