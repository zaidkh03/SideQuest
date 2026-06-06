using System.Globalization;
using SideQuest.Contracts;
using SideQuest.Models;
using SideQuest.ViewModels;

namespace SideQuest.Controllers
{
    internal static class PortalPageMapping
    {
        public static PortalJobViewModel ToPortalJob(this JobResponse job)
        {
            return new PortalJobViewModel
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                CompanyName = job.CompanyName,
                CategoryName = job.CategoryName,
                RewardLabel = FormatReward(job.BudgetType, job.FixedBudget, job.HourlyRate),
                BudgetType = job.BudgetType,
                FixedBudget = job.FixedBudget,
                HourlyRate = job.HourlyRate,
                WorkersNeeded = job.WorkersNeeded,
                AcceptedWorkers = job.AcceptedWorkers,
                StartDate = job.StartDate,
                EndDate = job.EndDate,
                Status = job.Status,
                CreatedAt = job.CreatedAt
            };
        }

        public static PortalApplicationViewModel ToPortalApplication(this JobApplication application)
        {
            return new PortalApplicationViewModel
            {
                Id = application.Id,
                JobId = application.JobId,
                JobTitle = application.Job.Title,
                WorkerId = application.WorkerId,
                WorkerName = DisplayName(application.Worker),
                CompanyName = application.Job.Company.CompanyName,
                CoverLetter = application.CoverLetter,
                Status = application.Status,
                AppliedAt = application.AppliedAt
            };
        }

        public static PortalAssignmentViewModel ToPortalAssignment(this JobAssignment assignment)
        {
            return new PortalAssignmentViewModel
            {
                Id = assignment.Id,
                JobId = assignment.JobId,
                JobTitle = assignment.Job.Title,
                WorkerId = assignment.WorkerId,
                WorkerName = DisplayName(assignment.Worker),
                CompanyName = assignment.Job.Company.CompanyName,
                BudgetType = assignment.Job.BudgetType,
                AgreedRate = assignment.AgreedRate,
                HoursWorked = assignment.HoursWorked,
                Earnings = assignment.Earnings,
                IsCompleted = assignment.IsCompleted,
                CompletedAt = assignment.CompletedAt
            };
        }

        public static string DisplayName(ApplicationUser user)
        {
            return string.IsNullOrWhiteSpace(user.FullName)
                ? user.Email ?? "User"
                : user.FullName;
        }

        public static string Initials(string displayName)
        {
            var initials = string.Join(
                    string.Empty,
                    displayName
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Take(2)
                        .Select(part => part[0]))
                .ToUpperInvariant();

            return string.IsNullOrWhiteSpace(initials) ? "SQ" : initials;
        }

        private static string FormatReward(BudgetType budgetType, decimal fixedBudget, decimal hourlyRate)
        {
            return budgetType == BudgetType.Fixed
                ? fixedBudget.ToString("C0", CultureInfo.CurrentCulture)
                : $"{hourlyRate.ToString("C0", CultureInfo.CurrentCulture)}/hr";
        }
    }
}
