using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Authorization;
using SideQuest.Contracts;
using SideQuest.Services;

namespace SideQuest.Controllers.Api
{
    [Authorize(Roles = SideQuestRoles.Admin)]
    [Route("api/admin/achievements")]
    public class AdminAchievementsController : ApiControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminAchievementsController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AchievementResponse>>> Get()
        {
            return ToActionResult(await _adminService.GetAchievementsAsync());
        }

        [HttpPost]
        public async Task<ActionResult<AchievementResponse>> Create(AchievementRequest request)
        {
            return ToActionResult(await _adminService.CreateAchievementAsync(request));
        }

        [HttpPut("{achievementId:int}")]
        public async Task<ActionResult<AchievementResponse>> Update(int achievementId, AchievementRequest request)
        {
            return ToActionResult(await _adminService.UpdateAchievementAsync(achievementId, request));
        }
    }
}
