using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Authorization;
using SideQuest.Contracts;
using SideQuest.Services;

namespace SideQuest.Controllers.Api
{
    [Route("api/jobs")]
    public class JobsController : ApiControllerBase
    {
        private readonly IJobService _jobService;

        public JobsController(IJobService jobService)
        {
            _jobService = jobService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IReadOnlyList<JobResponse>>> GetOpenJobs([FromQuery] JobQueryParameters query)
        {
            return ToActionResult(await _jobService.GetOpenJobsAsync(query));
        }

        [HttpGet("mine")]
        [Authorize(Roles = SideQuestRoles.Employer)]
        public async Task<ActionResult<IReadOnlyList<JobResponse>>> GetMine()
        {
            return CurrentUserId is null
                ? UnauthorizedResult<IReadOnlyList<JobResponse>>()
                : ToActionResult(await _jobService.GetEmployerJobsAsync(CurrentUserId));
        }

        [HttpGet("admin")]
        [Authorize(Roles = SideQuestRoles.Admin)]
        public async Task<ActionResult<IReadOnlyList<JobResponse>>> GetAdminJobs([FromQuery] JobQueryParameters query)
        {
            return ToActionResult(await _jobService.GetAdminJobsAsync(query));
        }

        [HttpGet("{jobId:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<JobResponse>> GetById(int jobId)
        {
            return ToActionResult(await _jobService.GetJobAsync(jobId, CurrentUserId, User.IsInRole(SideQuestRoles.Admin)));
        }

        [HttpPost]
        [Authorize(Roles = SideQuestRoles.Employer)]
        public async Task<ActionResult<JobResponse>> Create(UpsertJobRequest request)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<JobResponse>()
                : ToActionResult(await _jobService.CreateJobAsync(CurrentUserId, request));
        }

        [HttpPut("{jobId:int}")]
        [Authorize(Roles = SideQuestRoles.Employer)]
        public async Task<ActionResult<JobResponse>> Update(int jobId, UpsertJobRequest request)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<JobResponse>()
                : ToActionResult(await _jobService.UpdateJobAsync(CurrentUserId, jobId, request));
        }

        [HttpPost("{jobId:int}/publish")]
        [Authorize(Roles = SideQuestRoles.Employer)]
        public async Task<ActionResult<JobResponse>> Publish(int jobId)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<JobResponse>()
                : ToActionResult(await _jobService.PublishJobAsync(CurrentUserId, jobId));
        }

        [HttpPost("{jobId:int}/cancel")]
        [Authorize(Roles = SideQuestRoles.Employer)]
        public async Task<ActionResult<JobResponse>> Cancel(int jobId)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<JobResponse>()
                : ToActionResult(await _jobService.CancelJobAsync(CurrentUserId, jobId));
        }

        [HttpPost("{jobId:int}/close")]
        [Authorize(Roles = SideQuestRoles.Employer)]
        public async Task<ActionResult<JobResponse>> Close(int jobId)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<JobResponse>()
                : ToActionResult(await _jobService.CloseJobAsync(CurrentUserId, jobId));
        }
    }
}
