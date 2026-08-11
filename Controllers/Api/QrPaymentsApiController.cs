using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Controllers.Api;

[ApiController, Route("api/payments/qr")]
[SessionAuthorize("Cashier,Admin")]
public class QrPaymentsApiController(
    ApplicationDbContext context,
    ILoyaltyService loyaltyService) : ControllerBase
{
    [HttpPost("intents")]
    [ValidateAntiForgeryTokenFromHeader]
    public async Task<IActionResult> CreateIntent([FromBody] CreateQrIntentRequest request)
    {
        if (!IsStaff()) return Unauthorized();
        var gateway = await context.PaymentGatewaySettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive && x.Provider == "VietQR");
        if (gateway is null)
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Payment gateway is not connected",
                detail: "Quán chưa bật cấu hình VietQR.");

        var receiver = await context.PaymentAccountSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Provider == "Placeholder" && x.IsActive);
        if (receiver is null)
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "QR payment placeholder is not configured",
                detail: "Admin chưa cấu hình tài khoản nhận tiền cho placeholder QR.");

        var activeOrders = await context.Orders
            .AsNoTracking()
            .Include(o => o.Payment)
            .Where(o => o.TableID == request.TableId && !o.IsDeleted &&
                        o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled" &&
                        o.OrderDetails.Any(d => !d.IsDeleted))
            .ToListAsync();
        if (activeOrders.Count > 1)
            return Conflict(new { message = "Bàn có nhiều đơn đang mở, không thể tạo mã QR tự động." });
        var order = activeOrders.SingleOrDefault();
        if (order is null)
            return NotFound(new { message = "Không có đơn hàng để tạo QR." });
        if (request.ExpectedOrderId.HasValue && request.ExpectedOrderId.Value != order.OrderID)
            return Conflict(new { message = "Đơn hàng trên bàn đã thay đổi. Vui lòng tải lại POS trước khi tạo QR." });
        if (order.Payment?.PaymentStatus == "Completed")
            return Conflict(new { message = "Đơn hàng đã thanh toán." });

        LoyaltyQuoteDto quote;
        try
        {
            quote = await loyaltyService.GetOrderQuoteAsync(order.OrderID, HttpContext.RequestAborted);
        }
        catch (LoyaltyRuleException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }

        var amount = quote.TotalAmount;
        if (amount == 0)
            return Conflict(new
            {
                message = "Đơn hàng còn 0 ₫, không cần tạo mã QR. Hãy hoàn tất đơn để in hóa đơn.",
                zeroAmount = true,
                orderId = order.OrderID
            });
        if (amount < 0 || decimal.Truncate(amount) != amount)
            return BadRequest(new { message = "VietQR yêu cầu số tiền nguyên không âm." });

        var payment = new Payment
        {
            OrderID = order.OrderID,
            Amount = amount,
            PaymentMethod = "QRPlaceholder",
            PaymentStatus = "Pending",
            PaymentDate = DateTime.UtcNow,
            TransactionCode = $"BP{order.OrderID}"
        };

        var qrImageUrl = BuildVietQrQuickLink(gateway.MerchantId, receiver, payment);

        return Ok(new
        {
            orderId = order.OrderID,
            paymentId = order.Payment?.PaymentID,
            amount,
            transferContent = payment.TransactionCode,
            qrImageUrl,
            receiverAccount = receiver.AccountNumber,
            receiverName = receiver.AccountName,
            provider = gateway.Provider,
            isPlaceholder = true,
            status = payment.PaymentStatus
        });
    }

    [HttpGet("status/{orderId:int}")]
    public async Task<IActionResult> Status(int orderId)
    {
        if (!IsStaff()) return Unauthorized();
        var payment = await context.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrderID == orderId && !p.IsDeleted);
        return payment is null
            ? NotFound()
            : Ok(new { orderId, paymentId = payment.PaymentID, payment.PaymentStatus, payment.Amount, payment.TransactionCode });
    }

    private bool IsStaff() => HttpContext.Session.GetString("RoleName") is "Admin" or "Cashier";

    private static string BuildVietQrQuickLink(
        string bankId,
        PaymentAccountSetting receiver,
        Payment payment)
    {
        var bank = Uri.EscapeDataString(bankId.Trim());
        var account = Uri.EscapeDataString(receiver.AccountNumber.Trim());
        var content = Uri.EscapeDataString(payment.TransactionCode ?? $"BP{payment.OrderID}");
        var accountName = Uri.EscapeDataString(receiver.AccountName.Trim());
        return $"https://img.vietqr.io/image/{bank}-{account}-compact2.png" +
               $"?amount={payment.Amount:0}&addInfo={content}&accountName={accountName}";
    }

    public sealed record CreateQrIntentRequest(int TableId, int? ExpectedOrderId = null);
}
