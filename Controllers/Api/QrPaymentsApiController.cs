using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Services;

namespace Quản_lý_quán_cafe.Controllers.Api;

[ApiController, Route("api/payments/qr")]
public class QrPaymentsApiController(
    ApplicationDbContext context,
    IOptions<QrPaymentOptions> options) : ControllerBase
{
    [HttpPost("intents")]
    public async Task<IActionResult> CreateIntent([FromBody] CreateQrIntentRequest request)
    {
        if (!IsStaff()) return Unauthorized();
        var receiver = await context.PaymentAccountSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Provider == "MoMo" && x.IsActive);
        if (receiver is null)
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "MoMo payment is not configured",
                detail: "Admin chưa cấu hình tài khoản nhận tiền MoMo.");

        var order = await context.Orders
            .Include(o => o.OrderDetails.Where(d => !d.IsDeleted))
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.TableID == request.TableId && !o.IsDeleted &&
                                      o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled");
        if (order is null || order.OrderDetails.Count == 0)
            return NotFound(new { message = "Không có đơn hàng để tạo QR." });
        if (order.Payment?.PaymentStatus == "Completed")
            return Conflict(new { message = "Đơn hàng đã thanh toán." });

        var subtotal = order.OrderDetails.Sum(d => d.Subtotal);
        var discount = CalculateDiscount(subtotal, request.DiscountType, request.DiscountValue);
        var amount = subtotal - discount;
        if (amount <= 0 || decimal.Truncate(amount) != amount)
            return BadRequest(new { message = "VietQR yêu cầu số tiền nguyên dương." });

        var payment = order.Payment ?? new Payment
        {
            OrderID = order.OrderID,
            CreatedAt = DateTime.UtcNow
        };
        payment.Amount = amount;
        payment.PaymentMethod = "MoMo";
        payment.PaymentStatus = "Pending";
        payment.PaymentDate = DateTime.UtcNow;
        payment.TransactionCode = $"BP{order.OrderID}";
        payment.UpdatedAt = DateTime.UtcNow;
        if (order.Payment is null) context.Payments.Add(payment);

        order.TotalAmount = amount;
        order.OrderStatus = "WaitingPayment";
        order.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        order.PaymentID = payment.PaymentID;
        await context.SaveChangesAsync();

        return Ok(new
        {
            orderId = order.OrderID,
            paymentId = payment.PaymentID,
            amount,
            transferContent = payment.TransactionCode,
            qrImageUrl = (string?)null,
            receiverAccount = receiver.AccountNumber,
            receiverName = receiver.AccountName,
            merchantApiConfigured = options.Value.HasMerchantCredentials,
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

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        [FromHeader(Name = "X-Webhook-Secret")] string? secret,
        [FromBody] QrWebhookRequest request)
    {
        var configuredSecret = options.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(configuredSecret) ||
            !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(secret ?? string.Empty),
                System.Text.Encoding.UTF8.GetBytes(configuredSecret)))
            return Unauthorized();

        var payment = await context.Payments
            .Include(p => p.Order)!.ThenInclude(o => o!.Table)
            .FirstOrDefaultAsync(p => p.OrderID == request.OrderId && !p.IsDeleted);
        if (payment is null) return NotFound();
        if (payment.PaymentStatus == "Completed") return Ok(new { success = true, duplicate = true });
        if (request.Amount != payment.Amount)
            return BadRequest(new { message = "Số tiền callback không khớp." });

        payment.PaymentStatus = "Completed";
        payment.TransactionCode = string.IsNullOrWhiteSpace(request.TransactionCode)
            ? payment.TransactionCode
            : request.TransactionCode.Trim();
        payment.PaymentDate = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        if (payment.Order is not null)
        {
            payment.Order.OrderStatus = "Completed";
            payment.Order.CompletedDate = DateTime.UtcNow;
            payment.Order.UpdatedAt = DateTime.UtcNow;
            if (payment.Order.Table is not null)
            {
                payment.Order.Table.TableStatus = "Available";
                payment.Order.Table.UpdatedAt = DateTime.UtcNow;
            }
        }
        await context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    private bool IsStaff() => HttpContext.Session.GetString("RoleName") is "Admin" or "Cashier";

    private static decimal CalculateDiscount(decimal subtotal, string? type, decimal value) =>
        type?.ToLowerInvariant() switch
        {
            "percent" => subtotal * Math.Clamp(value, 0, 100) / 100,
            "fixed" => Math.Clamp(value, 0, subtotal),
            _ => 0
        };

    public sealed record CreateQrIntentRequest(int TableId, string? DiscountType, decimal DiscountValue);
    public sealed record QrWebhookRequest(int OrderId, decimal Amount, string? TransactionCode);
}
