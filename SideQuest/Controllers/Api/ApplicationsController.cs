using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Authorization;
using SideQuest.Contracts;
using SideQuest.Services;

namespace SideQuest.Controllers.Api
{
    [Route("api")]
    public class ApplicationsController : ApiControllerBase
    {
        private readonly IApplicationService _applicationService;

        public ApplicationsController(IApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        [HttpGet("applications/mine")]
        [Authorize(Roles = SideQuestRoles.Worker)]
        public async Task<ActionResult<IReadOnlyList<JobApplicationResponse>>> GetMine()
        {
            return CurrentUserId is null
                ? UnauthorizedResult<IReadOnlyList<JobApplicationResponse>>()
                : ToActionResult(await _applicationService.GetWorkerApplicationsAsync(CurrentUserId));
        }

        [HttpGet("jobs/{jobId:int}/applications")]
        [Authorize(Roles = SideQuestRoles.Employer)]
        public async Task<ActionResult<IReadOnlyList<JobApplicationResponse>>> GetForJob(int jobId)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<IReadOnlyList<JobApplicationResponse>>()
                : ToActionResult(await _applicationService.GetJobApplicationsAsync(CurrentUserId, jobId));
        }

        [HttpPost("jobs/{jobId:int}/applications")]
        [Authorize(Roles = SideQuestRoles.Worker)]
        public async Task<ActionResult<JobApplicationResponse>> Apply(int jobId, CreateApplicationRequest request)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<JobApplicationResponse>()
                : ToActionResult(await _applicationService.ApplyAsync(CurrentUserId, jobId, request));
        }

        [HttpPost("applications/{applicationId:int}/withdraw")]
        [Authorize(Roles = SideQuestRoles.Worker)]
        public async Task<ActionResult<JobApplicationResponse>> Withdraw(int applicationId)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<JobApplicationResponse>()
                : ToActionResult(await _applicationService.WithdrawAsync(CurrentUserId, applicationId));
        }

        [HttpPost("applications/{applicationId:int}/accept")]
        [Authorize(Roles = SideQuestRoles.Employer)]
        public async Task<ActionResult<JobApplicationResponse>> Accept(int applicationId)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<JobApplicationResponse>()
                : ToActionResult(await _applicationService.AcceptAsync(CurrentUserId, applicationId));
        }

        [HttpPost("applications/{applicationId:int}/reject")]
        [Authorize(Roles = SideQuestRoles.Employer)]
        public async Task<ActionResult<JobApplicationResponse>> Reject(int applicationId)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<JobApplicationResponse>()
                : ToActionResult(await _applicationService.RejectAsync(CurrentUserId, applicationId));
        }
    }
}
