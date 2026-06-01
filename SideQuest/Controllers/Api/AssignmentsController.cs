using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Authorization;
using SideQuest.Contracts;
using SideQuest.Services;

namespace SideQuest.Controllers.Api
{
    [Route("api/assignments")]
    public class AssignmentsController : ApiControllerBase
    {
        private readonly IAssignmentService _assignmentService;

        public AssignmentsController(IAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        [HttpGet("mine")]
        [Authorize(Roles = SideQuestRoles.Worker)]
        public async Task<ActionResult<IReadOnlyList<JobAssignmentResponse>>> GetMine()
        {
            return CurrentUserId is null
                ? UnauthorizedResult<IReadOnlyList<JobAssignmentResponse>>()
                : ToActionResult(await _assignmentService.GetWorkerAssignmentsAsync(CurrentUserId));
        }

        [HttpGet("employer")]
        [Authorize(Roles = SideQuestRoles.Employer)]
        public async Task<ActionResult<IReadOnlyList<JobAssignmentResponse>>> GetEmployerAssignments()
        {
            return CurrentUserId is null
                ? UnauthorizedResult<IReadOnlyList<JobAssignmentResponse>>()
                : ToActionResult(await _assignmentService.GetEmployerAssignmentsAsync(CurrentUserId));
        }

        [HttpPost("{assignmentId:int}/complete")]
        [Authorize(Roles = SideQuestRoles.Employer)]
        public async Task<ActionResult<JobAssignmentResponse>> Complete(int assignmentId, CompleteAssignmentRequest request)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<JobAssignmentResponse>()
                : ToActionResult(await _assignmentService.CompleteAsync(CurrentUserId, assignmentId, request));
        }
    }
}
