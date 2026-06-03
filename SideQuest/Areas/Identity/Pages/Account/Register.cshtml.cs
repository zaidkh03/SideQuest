using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SideQuest.Models;

namespace SideQuest.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private static readonly HashSet<string> AllowedAccountTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Worker",
            "Company"
        };

        private readonly ILogger<RegisterModel> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public void OnGet(string? accountType = null, string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            Input.AccountType = IsAllowedAccountType(accountType) ? accountType! : "Worker";
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!IsAllowedAccountType(Input.AccountType))
            {
                ModelState.AddModelError(nameof(Input.AccountType), "Choose Worker or Company to continue.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                EmailConfirmed = true,
                FullName = Input.FullName.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new SideQuest account.");
                await _signInManager.SignInAsync(user, isPersistent: false);

                return string.Equals(Input.AccountType, "Company", StringComparison.OrdinalIgnoreCase)
                    ? RedirectToAction("Company", "Onboarding", new { area = string.Empty })
                    : RedirectToAction("Worker", "Onboarding", new { area = string.Empty });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        private static bool IsAllowedAccountType(string? accountType)
        {
            return !string.IsNullOrWhiteSpace(accountType) && AllowedAccountTypes.Contains(accountType);
        }

        public sealed class InputModel
        {
            [Required]
            public string AccountType { get; set; } = "Worker";

            [Required]
            [MaxLength(100)]
            [Display(Name = "Full name")]
            public string FullName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}
