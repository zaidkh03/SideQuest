using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Authorization;
using SideQuest.Contracts;
using SideQuest.Services;

namespace SideQuest.Controllers.Api
{
    [Authorize(Roles = SideQuestRoles.Worker)]
    [Route("api/wallet")]
    public class WalletController : ApiControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet]
        public async Task<ActionResult<WalletResponse>> Get()
        {
            return CurrentUserId is null
                ? UnauthorizedResult<WalletResponse>()
                : ToActionResult(await _walletService.GetWalletAsync(CurrentUserId));
        }

        [HttpPost("withdrawals")]
        public async Task<ActionResult<TransactionResponse>> RequestWithdrawal(CreateWithdrawalRequest request)
        {
            return CurrentUserId is null
                ? UnauthorizedResult<TransactionResponse>()
                : ToActionResult(await _walletService.RequestWithdrawalAsync(CurrentUserId, request));
        }
    }
}
