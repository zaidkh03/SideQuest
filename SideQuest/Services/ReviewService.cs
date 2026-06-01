using Microsoft.EntityFrameworkCore;
using SideQuest.Contracts;
using SideQuest.Data;
using SideQuest.Models;

namespace SideQuest.Services
{
    public interface IReviewService
    {
        Task<ServiceResult<ReviewResponse>> CreateReviewAsync(string reviewerUserId, CreateReviewRequest request);
    }

    public class ReviewService : IReviewService
    {
        private readonly AppDbContext _context;

        public ReviewService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<ReviewResponse>> CreateReviewAsync(string reviewerUserId, CreateReviewRequest request)
        {
            if (reviewerUserId == request.ReviewedUserId)
            {
                return ServiceResult<ReviewResponse>.Conflict("Users cannot review themselves.");
            }

            var job = await _context.Jobs
                .Include(existingJob => existingJob.Company)
                .Include(existingJob => existingJob.Assignments)
                .Include(existingJob => existingJob.Reviews)
                .FirstOrDefaultAsync(existingJob => existingJob.Id == request.JobId);

            if (job is null)
            {
                return ServiceResult<ReviewResponse>.NotFound("Job was not found.");
            }

            if (job.Status is not (JobStatus.WaitingForReview or JobStatus.Completed))
            {
                return ServiceResult<ReviewResponse>.Conflict("Reviews are allowed only after the job has completed work.");
            }

            if (!await _context.Users.AnyAsync(user => user.Id == request.ReviewedUserId))
            {
                return ServiceResult<ReviewResponse>.NotFound("Reviewed user was not found.");
            }

            var reviewerIsEmployer = job.Company.UserId == reviewerUserId;
            var reviewerAssignment = job.Assignments.FirstOrDefault(assignment => assignment.WorkerId == reviewerUserId);
            var reviewedAssignment = job.Assignments.FirstOrDefault(assignment => assignment.WorkerId == request.ReviewedUserId);

            var employerReviewingCompletedWorker = reviewerIsEmployer && reviewedAssignment?.IsCompleted == true;
            var completedWorkerReviewingEmployer =
                reviewerAssignment?.IsCompleted == true &&
                request.ReviewedUserId == job.Company.UserId;

            if (!employerReviewingCompletedWorker && !completedWorkerReviewingEmployer)
            {
                return ServiceResult<ReviewResponse>.Forbidden("Only completed job participants can review each other.");
            }

            if (job.Reviews.Any(review =>
                    review.ReviewerId == reviewerUserId &&
                    review.ReviewedUserId == request.ReviewedUserId))
            {
                return ServiceResult<ReviewResponse>.Conflict("This review already exists.");
            }

            var review = new Review
            {
                JobId = job.Id,
                ReviewerId = reviewerUserId,
                ReviewedUserId = request.ReviewedUserId,
                Rating = request.Rating,
                Comment = request.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            AddNotification(
                request.ReviewedUserId,
                "New review received",
                $"You received a review for {job.Title}.",
                "ReviewReceived");

            await _context.SaveChangesAsync();
            await RefreshWorkerAverageRatingAsync(request.ReviewedUserId);
            await _context.SaveChangesAsync();

            var savedReview = await _context.Reviews
                .Include(existingReview => existingReview.Reviewer)
                .Include(existingReview => existingReview.ReviewedUser)
                .FirstAsync(existingReview => existingReview.Id == review.Id);

            return ServiceResult<ReviewResponse>.Created(savedReview.ToResponse());
        }

        private async Task RefreshWorkerAverageRatingAsync(string reviewedUserId)
        {
            var workerProfile = await _context.WorkerProfiles
                .FirstOrDefaultAsync(profile => profile.UserId == reviewedUserId);

            if (workerProfile is null)
            {
                return;
            }

            var ratings = await _context.Reviews
                .Where(review => review.ReviewedUserId == reviewedUserId)
                .Select(review => review.Rating)
                .ToListAsync();

            workerProfile.AverageRating = ratings.Count == 0
                ? 0
                : decimal.Round((decimal)ratings.Average(), 2);
            workerProfile.UpdatedAt = DateTime.UtcNow;
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
