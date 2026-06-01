using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Authorization;
using SideQuest.Contracts;
using SideQuest.Services;

namespace SideQuest.Controllers.Api
{
    [Authorize(Roles = SideQuestRoles.Admin)]
    [Route("api/admin/users")]
    public class AdminUsersController : ApiControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminUsersController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> Get()
        {
            return ToActionResult(await _adminService.GetUsersAsync());
        }

        [HttpPut("{userId}/status")]
        public async Task<ActionResult<AdminUserResponse>> UpdateStatus(string userId, UpdateUserStatusRequest request)
        {
            return ToActionResult(await _adminService.UpdateUserStatusAsync(userId, request));
        }
    }
}
