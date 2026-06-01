using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Contracts;
using SideQuest.Services;

namespace SideQuest.Controllers.Api
{
    [Authorize]
    [Route("api/company-profile")]
    public class CompanyProfilesController : ApiControllerBase
    {
        private readonly IProfileService _profileService;

        public CompanyProfilesController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet]
        public async Task<ActionResult<CompanyProfileResponse>> Get()
        {
            return CurrentUserId is null
                ? UnauthorizedResult<CompanyProfileResponse>()
                : ToActionResult(await _profileService.GetCompanyProfileAsync(CurrentUserId));
        }

        [HttpPut]
        public async Task<ActionResult<CompanyProfileResponse>> Upsert(UpsertCompanyProfileRequest request)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<CompanyProfileResponse>()
                : ToActionResult(await _profileService.UpsertCompanyProfileAsync(CurrentUserId, request));
        }
    }
}
