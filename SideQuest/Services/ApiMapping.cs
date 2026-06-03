using SideQuest.Contracts;
using SideQuest.Models;

namespace SideQuest.Services
{
    internal static class ApiMapping
    {
        public static WorkerProfileResponse ToResponse(this WorkerProfile profile)
        {
            return new WorkerProfileResponse
            {
                Id = profile.Id,
                UserId = profile.UserId,
                FullName = profile.User.FullName,
                Headline = profile.Headline,
                Bio = profile.Bio,
                Location = profile.Location,
                HourlyRatePreference = profile.HourlyRatePreference,
                AvailabilityStatus = profile.AvailabilityStatus,
                VerificationStatus = profile.VerificationStatus,
                VerificationSubmittedAt = profile.VerificationSubmittedAt,
                VerificationReviewedAt = profile.VerificationReviewedAt,
                VerificationRejectionReason = profile.VerificationRejectionReason,
                VerificationRejectionMessage = profile.VerificationRejectionMessage,
                PortfolioUrl = profile.PortfolioUrl,
                ResumeUrl = profile.ResumeUrl,
                ExperienceYears = profile.ExperienceYears,
                TotalJobsCompleted = profile.TotalJobsCompleted,
                AverageRating = profile.AverageRating,
                Skills = profile.User.UserSkills
                    .OrderBy(userSkill => userSkill.Skill.Name)
                    .Select(userSkill => new UserSkillResponse
                    {
                        SkillId = userSkill.SkillId,
                        Name = userSkill.Skill.Name,
                        SkillLevel = userSkill.SkillLevel
                    })
                    .ToList()
            };
        }

        public static CompanyProfileResponse ToResponse(this CompanyProfile profile)
        {
            var activeSubscription = profile.CompanySubscriptions
                .Where(subscription => subscription.IsActive)
                .OrderByDescending(subscription => subscription.StartDate)
                .FirstOrDefault();

            return new CompanyProfileResponse
            {
                Id = profile.Id,
                UserId = profile.UserId,
                CompanyName = profile.CompanyName,
                Description = profile.Description,
                Location = profile.Location,
                Website = profile.Website,
                LogoUrl = profile.LogoUrl,
                IsVerified = profile.IsVerified,
                VerificationStatus = profile.VerificationStatus,
                VerificationSubmittedAt = profile.VerificationSubmittedAt,
                VerificationReviewedAt = profile.VerificationReviewedAt,
                VerificationRejectionReason = profile.VerificationRejectionReason,
                VerificationRejectionMessage = profile.VerificationRejectionMessage,
                ActiveSubscription = activeSubscription is null
                    ? null
                    : new SubscriptionSummaryResponse
                    {
                        PlanName = activeSubscription.Plan.Name,
                        JobLimitPerMonth = activeSubscription.Plan.JobLimitPerMonth,
                        CommissionRate = activeSubscription.Plan.CommissionRate,
                        StartDate = activeSubscription.StartDate,
                        EndDate = activeSubscription.EndDate
                    }
            };
        }

        public static JobResponse ToResponse(this Job job)
        {
            return new JobResponse
            {
                Id = job.Id,
                CompanyId = job.CompanyId,
                CompanyName = job.Company.CompanyName,
                CategoryId = job.CategoryId,
                CategoryName = job.Category.Name,
                Title = job.Title,
                Description = job.Description,
                BudgetType = job.BudgetType,
                FixedBudget = job.FixedBudget,
                HourlyRate = job.HourlyRate,
                WorkersNeeded = job.WorkersNeeded,
                AcceptedWorkers = job.Assignments.Count,
                StartDate = job.StartDate,
                EndDate = job.EndDate,
                Status = job.Status,
                CreatedAt = job.CreatedAt
            };
        }

        public static JobApplicationResponse ToResponse(this JobApplication application)
        {
            return new JobApplicationResponse
            {
                Id = application.Id,
                JobId = application.JobId,
                JobTitle = application.Job.Title,
                WorkerId = application.WorkerId,
                WorkerName = application.Worker.FullName,
                CoverLetter = application.CoverLetter,
                Status = application.Status,
                AppliedAt = application.AppliedAt
            };
        }

        public static JobAssignmentResponse ToResponse(this JobAssignment assignment)
        {
            return new JobAssignmentResponse
            {
                Id = assignment.Id,
                JobId = assignment.JobId,
                JobTitle = assignment.Job.Title,
                WorkerId = assignment.WorkerId,
                WorkerName = assignment.Worker.FullName,
                AgreedRate = assignment.AgreedRate,
                HoursWorked = assignment.HoursWorked,
                Earnings = assignment.Earnings,
                IsCompleted = assignment.IsCompleted,
                CompletedAt = assignment.CompletedAt
            };
        }

        public static ReviewResponse ToResponse(this Review review)
        {
            return new ReviewResponse
            {
                Id = review.Id,
                JobId = review.JobId,
                ReviewerId = review.ReviewerId,
                ReviewerName = review.Reviewer.FullName,
                ReviewedUserId = review.ReviewedUserId,
                ReviewedUserName = review.ReviewedUser.FullName,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            };
        }

        public static NotificationResponse ToResponse(this Notification notification)
        {
            return new NotificationResponse
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }

        public static TransactionResponse ToResponse(this Transaction transaction)
        {
            return new TransactionResponse
            {
                Id = transaction.Id,
                JobId = transaction.JobId,
                Amount = transaction.Amount,
                Type = transaction.Type,
                Status = transaction.Status,
                CreatedAt = transaction.CreatedAt
            };
        }

        public static CategoryResponse ToResponse(this Category category)
        {
            return new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };
        }

        public static AchievementResponse ToResponse(this Achievement achievement)
        {
            return new AchievementResponse
            {
                Id = achievement.Id,
                Name = achievement.Name,
                Description = achievement.Description,
                XPRequired = achievement.XPRequired,
                BadgeImageUrl = achievement.BadgeImageUrl
            };
        }
    }
}
