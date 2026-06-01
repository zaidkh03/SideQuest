using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SideQuest.Contracts;
using SideQuest.Data;
using SideQuest.Models;

namespace SideQuest.Services
{
    public interface IAccountService
    {
        Task<CurrentUserResponse> GetCurrentUserAsync(string? userId);
    }

    public class AccountService : IAccountService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountService(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<CurrentUserResponse> GetCurrentUserAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new CurrentUserResponse();
            }

            var user = await _context.Users
                .Include(applicationUser => applicationUser.WorkerProfile)
                .Include(applicationUser => applicationUser.CompanyProfile)
                .Include(applicationUser => applicationUser.Wallet)
                .FirstOrDefaultAsync(applicationUser => applicationUser.Id == userId);

            if (user is null)
            {
                return new CurrentUserResponse();
            }

            return new CurrentUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                IsAuthenticated = true,
                Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                HasWorkerProfile = user.WorkerProfile is not null,
                HasCompanyProfile = user.CompanyProfile is not null,
                HasWallet = user.Wallet is not null
            };
        }
    }
}
