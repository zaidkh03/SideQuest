using Microsoft.EntityFrameworkCore;
using SideQuest.Contracts;
using SideQuest.Data;
using SideQuest.Models;

namespace SideQuest.Services
{
    public interface IWalletService
    {
        Task<ServiceResult<WalletResponse>> GetWalletAsync(string userId);

        Task<ServiceResult<TransactionResponse>> RequestWithdrawalAsync(string userId, CreateWithdrawalRequest request);
    }

    public class WalletService : IWalletService
    {
        private readonly AppDbContext _context;

        public WalletService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<WalletResponse>> GetWalletAsync(string userId)
        {
            var wallet = await GetOrCreateWalletAsync(userId);
            await _context.SaveChangesAsync();

            var transactions = await _context.Transactions
                .Where(transaction => transaction.UserId == userId)
                .OrderByDescending(transaction => transaction.CreatedAt)
                .Take(20)
                .ToListAsync();

            return ServiceResult<WalletResponse>.Success(new WalletResponse
            {
                CurrentBalance = wallet.CurrentBalance,
                TotalEarned = wallet.TotalEarned,
                TotalWithdrawn = wallet.TotalWithdrawn,
                RecentTransactions = transactions.Select(transaction => transaction.ToResponse()).ToList()
            });
        }

        public async Task<ServiceResult<TransactionResponse>> RequestWithdrawalAsync(string userId, CreateWithdrawalRequest request)
        {
            var wallet = await GetOrCreateWalletAsync(userId);

            if (request.Amount > wallet.CurrentBalance)
            {
                return ServiceResult<TransactionResponse>.Conflict("Withdrawal amount exceeds available wallet balance.");
            }

            wallet.CurrentBalance -= request.Amount;
            wallet.TotalWithdrawn += request.Amount;

            var transaction = new Transaction
            {
                UserId = userId,
                JobId = null,
                Amount = request.Amount,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = "Withdrawal requested",
                Message = "Your withdrawal request has been recorded.",
                Type = "WithdrawalRequested",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return ServiceResult<TransactionResponse>.Created(transaction.ToResponse());
        }

        private async Task<Wallet> GetOrCreateWalletAsync(string userId)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(existingWallet => existingWallet.UserId == userId);
            if (wallet is not null)
            {
                return wallet;
            }

            wallet = new Wallet { UserId = userId };
            _context.Wallets.Add(wallet);
            return wallet;
        }
    }
}
