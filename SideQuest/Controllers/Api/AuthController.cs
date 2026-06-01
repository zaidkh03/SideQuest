using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Contracts;
using SideQuest.Services;

namespace SideQuest.Controllers.Api
{
    [Route("api/auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAccountService _accountService;

        public AuthController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("me")]
        [AllowAnonymous]
        public async Task<ActionResult<CurrentUserResponse>> Me()
        {
            return Ok(await _accountService.GetCurrentUserAsync(CurrentUserId));
        }
    }
}
