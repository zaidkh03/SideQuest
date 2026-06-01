using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Contracts;
using SideQuest.Services;

namespace SideQuest.Controllers.Api
{
    [Authorize]
    [Route("api/worker-profile")]
    public class WorkerProfilesController : ApiControllerBase
    {
        private readonly IProfileService _profileService;

        public WorkerProfilesController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet]
        public async Task<ActionResult<WorkerProfileResponse>> Get()
        {
            return CurrentUserId is null
                ? UnauthorizedResult<WorkerProfileResponse>()
                : ToActionResult(await _profileService.GetWorkerProfileAsync(CurrentUserId));
        }

        [HttpPut]
        public async Task<ActionResult<WorkerProfileResponse>> Upsert(UpsertWorkerProfileRequest request)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<WorkerProfileResponse>()
                : ToActionResult(await _profileService.UpsertWorkerProfileAsync(CurrentUserId, request));
        }

        [HttpPut("skills")]
        public async Task<ActionResult<WorkerProfileResponse>> UpdateSkills(UpdateWorkerSkillsRequest request)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<WorkerProfileResponse>()
                : ToActionResult(await _profileService.UpdateWorkerSkillsAsync(CurrentUserId, request));
        }
    }
}
