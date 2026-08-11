using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Controllers.Api;

[ApiController]
[Route("api/orders/{orderId:int}/discounts")]
[SessionAuthorize("Customer,Cashier,Admin")]
public sealed class OrderDiscountsApiController(
    ApplicationDbContext context,
    ILoyaltyService loyaltyService,
    ILogger<OrderDiscountsApiController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int orderId, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(orderId, cancellationToken);
        if (access.Error is not null) return access.Error;

        try
        {
            return Ok(await loyaltyService.GetOrderQuoteAsync(orderId, cancellationToken));
        }
        catch (LoyaltyRuleException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPost("points")]
    [ValidateAntiForgeryTokenFromHeader]
    public async Task<IActionResult> ApplyPoints(
        int orderId,
        [FromBody] ApplyPointsRequest? request,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(orderId, cancellationToken);
        if (access.Error is not null) return access.Error;

        IReadOnlyCollection<int> customerIds;
        if (access.OwnerCustomerId.HasValue)
        {
            customerIds = new[] { access.OwnerCustomerId.Value };
        }
        else
        {
            customerIds = request?.CustomerIds?
                .Where(id => id > 0)
                .Distinct()
                .Take(10)
                .ToArray() ?? [];
        }

        if (customerIds.Count == 0)
            return BadRequest(new { message = "Hãy chọn ít nhất một tài khoản điểm." });

        return await ExecuteAsync(() => loyaltyService.ApplyPointsAsync(
            orderId,
            customerIds,
            access.ActorUserId,
            access.OwnerCustomerId,
            cancellationToken));
    }

    [HttpPost("voucher")]
    [ValidateAntiForgeryTokenFromHeader]
    public async Task<IActionResult> ApplyVoucher(
        int orderId,
        [FromBody] ApplyVoucherRequest? request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Code))
            return BadRequest(new { message = "Vui lòng nhập mã voucher." });

        var access = await GetAccessAsync(orderId, cancellationToken);
        if (access.Error is not null) return access.Error;

        var voucherCustomerId = access.OwnerCustomerId ?? request.CustomerId;
        if (!voucherCustomerId.HasValue)
        {
            return BadRequest(new
            {
                message = "Hãy tìm và chọn tài khoản khách hàng trước khi áp dụng voucher."
            });
        }

        return await ExecuteAsync(() => loyaltyService.ApplyVoucherAsync(
            orderId,
            request.Code,
            access.ActorUserId,
            access.OwnerCustomerId,
            voucherCustomerId,
            cancellationToken));
    }

    [HttpDelete]
    [ValidateAntiForgeryTokenFromHeader]
    public async Task<IActionResult> Clear(int orderId, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(orderId, cancellationToken);
        if (access.Error is not null) return access.Error;

        return await ExecuteAsync(() => loyaltyService.ClearDiscountAsync(
            orderId,
            access.ActorUserId,
            access.OwnerCustomerId,
            cancellationToken));
    }

    private async Task<IActionResult> ExecuteAsync(Func<Task<LoyaltyQuoteDto>> operation)
    {
        try
        {
            return Ok(await operation());
        }
        catch (LoyaltyRuleException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "A loyalty mutation conflicted with another request.");
            return Conflict(new { message = "Điểm hoặc đơn hàng vừa thay đổi. Vui lòng tải lại và thử lại." });
        }
    }

    private async Task<(int ActorUserId, int? OwnerCustomerId, IActionResult? Error)> GetAccessAsync(
        int orderId,
        CancellationToken cancellationToken)
    {
        var actorUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var role = HttpContext.Session.GetString("RoleName");
        if (actorUserId <= 0)
            return (0, null, Unauthorized());

        if (role is "Admin" or "Cashier")
        {
            var exists = await context.Orders.AsNoTracking()
                .AnyAsync(order => order.OrderID == orderId && !order.IsDeleted, cancellationToken);
            return exists
                ? (actorUserId, null, null)
                : (actorUserId, null, NotFound(new { message = "Không tìm thấy đơn hàng." }));
        }

        var customerId = await context.Users.AsNoTracking()
            .Where(user => user.UserID == actorUserId && user.CustomerID.HasValue && !user.IsDeleted)
            .Select(user => user.CustomerID)
            .SingleOrDefaultAsync(cancellationToken);
        if (!customerId.HasValue)
            return (actorUserId, null, Forbid());

        var ownsOrder = await context.Orders.AsNoTracking().AnyAsync(
            order => order.OrderID == orderId && order.CustomerID == customerId.Value && !order.IsDeleted,
            cancellationToken);
        return ownsOrder
            ? (actorUserId, customerId.Value, null)
            : (actorUserId, customerId.Value, NotFound(new { message = "Không tìm thấy đơn hàng." }));
    }

    public sealed record ApplyPointsRequest(IReadOnlyCollection<int>? CustomerIds);
    public sealed record ApplyVoucherRequest(string Code, int? CustomerId = null);
}
