using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Contracts;
using SideQuest.Services;

namespace SideQuest.Controllers.Api
{
    [Authorize]
    [Route("api/notifications")]
    public class NotificationsController : ApiControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> Get([FromQuery] bool unreadOnly = false)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<IReadOnlyList<NotificationResponse>>()
                : ToActionResult(await _notificationService.GetNotificationsAsync(CurrentUserId, unreadOnly));
        }

        [HttpPost("{notificationId:int}/read")]
        public async Task<ActionResult<NotificationResponse>> MarkRead(int notificationId)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<NotificationResponse>()
                : ToActionResult(await _notificationService.MarkReadAsync(CurrentUserId, notificationId));
        }
    }
}
