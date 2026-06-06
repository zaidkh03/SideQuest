using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Authorization;
using SideQuest.Models;
using SideQuest.Services;
using SideQuest.ViewModels;

namespace SideQuest.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(bool unreadOnly = false)
        {
            SetNotificationsViewData();

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var result = await _notificationService.GetNotificationsAsync(userId, unreadOnly);
            return View(new NotificationsPageViewModel
            {
                UnreadOnly = unreadOnly,
                Notifications = result.Value ?? []
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id, bool unreadOnly = false)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var result = await _notificationService.MarkReadAsync(userId, id);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "Notification marked read."
                : result.Message ?? "We could not update that notification.";

            return RedirectToAction(nameof(Index), new { unreadOnly });
        }

        private void SetNotificationsViewData()
        {
            ViewData["Title"] = "Notifications";
            ViewData["TopBarTitle"] = "Notifications";
            ViewData["Shell"] = "App";
            ViewData["ActivePortal"] = User.IsInRole(SideQuestRoles.Admin)
                ? "Admin"
                : User.IsInRole(SideQuestRoles.Employer)
                    ? "Employer"
                    : User.IsInRole(SideQuestRoles.Worker)
                        ? "Worker"
                        : "Onboarding";
            ViewData["ActiveNav"] = "Notifications";
        }
    }
}
