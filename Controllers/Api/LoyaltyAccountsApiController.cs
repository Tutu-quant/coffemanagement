using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Controllers.Api;

[ApiController]
[Route("api/loyalty/accounts")]
[SessionAuthorize("Cashier,Admin")]
public sealed class LoyaltyAccountsApiController(ILoyaltyService loyaltyService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? query, CancellationToken cancellationToken)
    {
        query = query?.Trim();
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return BadRequest(new { message = "Nhập ít nhất 2 ký tự để tìm tài khoản." });

        var accounts = await loyaltyService.SearchAccountsAsync(query, 10, cancellationToken);
        return Ok(new { accounts });
    }
}
