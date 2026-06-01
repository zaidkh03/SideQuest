using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Contracts;
using SideQuest.Services;

namespace SideQuest.Controllers.Api
{
    [Authorize]
    [Route("api/reviews")]
    public class ReviewsController : ApiControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        public async Task<ActionResult<ReviewResponse>> Create(CreateReviewRequest request)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<ReviewResponse>()
                : ToActionResult(await _reviewService.CreateReviewAsync(CurrentUserId, request));
        }
    }
}
