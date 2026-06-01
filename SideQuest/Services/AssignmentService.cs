using Microsoft.EntityFrameworkCore;
using SideQuest.Contracts;
using SideQuest.Data;
using SideQuest.Models;

namespace SideQuest.Services
{
    public interface IAssignmentService
    {
        Task<ServiceResult<IReadOnlyList<JobAssignmentResponse>>> GetWorkerAssignmentsAsync(string workerUserId);

        Task<ServiceResult<IReadOnlyList<JobAssignmentResponse>>> GetEmployerAssignmentsAsync(string employerUserId);

        Task<ServiceResult<JobAssignmentResponse>> CompleteAsync(string employerUserId, int assignmentId, CompleteAssignmentRequest request);
    }

    public class AssignmentService : IAssignmentService
    {
        private const int XpPerCompletedAssignment = 100;
        private const int XpPerLevel = 500;
        private const decimal DefaultCommissionRate = 10m;

        private readonly AppDbContext _context;

        public AssignmentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<IReadOnlyList<JobAssignmentResponse>>> GetWorkerAssignmentsAsync(string workerUserId)
        {
            var assignments = await GetAssignmentQuery()
                .Where(assignment => assignment.WorkerId == workerUserId)
                .OrderByDescending(assignment => assignment.Job.CreatedAt)
                .ToListAsync();

            return ServiceResult<IReadOnlyList<JobAssignmentResponse>>.Success(
                assignments.Select(assignment => assignment.ToResponse()).ToList());
        }

        public async Task<ServiceResult<IReadOnlyList<JobAssignmentResponse>>> GetEmployerAssignmentsAsync(string employerUserId)
        {
            var assignments = await GetAssignmentQuery()
                .Where(assignment => assignment.Job.Company.UserId == employerUserId)
                .OrderByDescending(assignment => assignment.Job.CreatedAt)
                .ToListAsync();

            return ServiceResult<IReadOnlyList<JobAssignmentResponse>>.Success(
                assignments.Select(assignment => assignment.ToResponse()).ToList());
        }

        public async Task<ServiceResult<JobAssignmentResponse>> CompleteAsync(string employerUserId, int assignmentId, CompleteAssignmentRequest request)
        {
            var assignment = await GetAssignmentQuery()
                .Include(existingAssignment => existingAssignment.Job.Assignments)
                .Include(existingAssignment => existingAssignment.Job.Commission)
                .Include(existingAssignment => existingAssignment.Job.Company.CompanySubscriptions)
                    .ThenInclude(subscription => subscription.Plan)
                .FirstOrDefaultAsync(existingAssignment => existingAssignment.Id == assignmentId);

            if (assignment is null)
            {
                return ServiceResult<JobAssignmentResponse>.NotFound("Assignment was not found.");
            }

            if (assignment.Job.Company.UserId != employerUserId)
            {
                return ServiceResult<JobAssignmentResponse>.Forbidden("Only the job owner can complete assignments.");
            }

            if (assignment.IsCompleted)
            {
                return ServiceResult<JobAssignmentResponse>.Conflict("Assignment is already completed.");
            }

            var hoursWorked = request.HoursWorked ?? 0;
            if (assignment.Job.BudgetType == BudgetType.Hourly && hoursWorked <= 0)
            {
                return ServiceResult<JobAssignmentResponse>.Validation(nameof(request.HoursWorked), "Hours worked is required for hourly jobs.");
            }

            var earnings = assignment.Job.BudgetType == BudgetType.Fixed
                ? assignment.AgreedRate
                : decimal.Round(assignment.AgreedRate * hoursWorked, 2);

            assignment.HoursWorked = hoursWorked;
            assignment.Earnings = earnings;
            assignment.IsCompleted = true;
            assignment.CompletedAt = DateTime.UtcNow;

            await ApplyWorkerLedgerAsync(assignment.WorkerId, assignment.JobId, earnings);
            ApplyCommissionLedger(assignment, employerUserId, earnings);
            await ApplyGamificationAsync(assignment.WorkerId);

            var workerProfile = await _context.WorkerProfiles
                .FirstOrDefaultAsync(profile => profile.UserId == assignment.WorkerId);
            if (workerProfile is not null)
            {
                workerProfile.TotalJobsCompleted += 1;
                workerProfile.UpdatedAt = DateTime.UtcNow;
            }

            AddNotification(
                assignment.WorkerId,
                "Assignment completed",
                $"Your assignment for {assignment.Job.Title} was marked complete.",
                "AssignmentCompleted");
            AddNotification(
                employerUserId,
                "Assignment completed",
                $"An assignment for {assignment.Job.Title} was completed and recorded.",
                "AssignmentCompleted");

            if (assignment.Job.Assignments.All(existingAssignment => existingAssignment.IsCompleted || existingAssignment.Id == assignment.Id))
            {
                assignment.Job.Status = JobStatus.WaitingForReview;
            }

            await _context.SaveChangesAsync();

            var savedAssignment = await GetAssignmentQuery()
                .FirstAsync(saved => saved.Id == assignment.Id);

            return ServiceResult<JobAssignmentResponse>.Success(savedAssignment.ToResponse());
        }

        private IQueryable<JobAssignment> GetAssignmentQuery()
        {
            return _context.JobAssignments
                .Include(assignment => assignment.Job)
                    .ThenInclude(job => job.Company)
                .Include(assignment => assignment.Worker);
        }

        private async Task ApplyWorkerLedgerAsync(string workerUserId, int jobId, decimal earnings)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(existingWallet => existingWallet.UserId == workerUserId);
            if (wallet is null)
            {
                wallet = new Wallet { UserId = workerUserId };
                _context.Wallets.Add(wallet);
            }

            wallet.CurrentBalance += earnings;
            wallet.TotalEarned += earnings;

            _context.Transactions.Add(new Transaction
            {
                UserId = workerUserId,
                JobId = jobId,
                Amount = earnings,
                Type = TransactionType.Earning,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            });
        }

        private void ApplyCommissionLedger(JobAssignment assignment, string employerUserId, decimal earnings)
        {
            var commissionRate = assignment.Job.Company.CompanySubscriptions
                .Where(subscription => subscription.IsActive)
                .OrderByDescending(subscription => subscription.StartDate)
                .Select(subscription => subscription.Plan.CommissionRate)
                .FirstOrDefault();

            if (commissionRate <= 0)
            {
                commissionRate = DefaultCommissionRate;
            }

            var commissionAmount = decimal.Round(earnings * commissionRate / 100m, 2);

            if (assignment.Job.Commission is null)
            {
                assignment.Job.Commission = new Commission
                {
                    JobId = assignment.JobId,
                    CompanyId = assignment.Job.CompanyId,
                    CommissionRate = commissionRate,
                    Amount = commissionAmount,
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                assignment.Job.Commission.Amount += commissionAmount;
                assignment.Job.Commission.CommissionRate = commissionRate;
            }

            _context.Transactions.Add(new Transaction
            {
                UserId = employerUserId,
                JobId = assignment.JobId,
                Amount = commissionAmount,
                Type = TransactionType.Commission,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            });
        }

        private async Task ApplyGamificationAsync(string workerUserId)
        {
            var userXp = await _context.UserXPs.FirstOrDefaultAsync(existingXp => existingXp.UserId == workerUserId);
            if (userXp is null)
            {
                userXp = new UserXP { UserId = workerUserId };
                _context.UserXPs.Add(userXp);
            }

            userXp.XP += XpPerCompletedAssignment;
            userXp.Level = (userXp.XP / XpPerLevel) + 1;

            var earnedAchievementIds = await _context.UserAchievements
                .Where(userAchievement => userAchievement.UserId == workerUserId)
                .Select(userAchievement => userAchievement.AchievementId)
                .ToListAsync();

            var unlockedAchievements = await _context.Achievements
                .Where(achievement => achievement.XPRequired <= userXp.XP && !earnedAchievementIds.Contains(achievement.Id))
                .ToListAsync();

            _context.UserAchievements.AddRange(unlockedAchievements.Select(achievement => new UserAchievement
            {
                UserId = workerUserId,
                AchievementId = achievement.Id,
                EarnedAt = DateTime.UtcNow
            }));
        }

        private void AddNotification(string userId, string title, string message, string type)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
