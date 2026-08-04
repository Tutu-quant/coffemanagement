using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Controllers.Api;

[ApiController, Route("api/payments/qr")]
public class QrPaymentsApiController(ApplicationDbContext context) : ControllerBase
{
    [HttpPost("intents")]
    public async Task<IActionResult> CreateIntent([FromBody] CreateQrIntentRequest request)
    {
        if (!IsStaff()) return Unauthorized();
        var receiver = await context.PaymentAccountSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Provider == "Placeholder" && x.IsActive);
        if (receiver is null)
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "QR payment placeholder is not configured",
                detail: "Admin chưa cấu hình tài khoản nhận tiền cho placeholder QR.");

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
            return BadRequest(new { message = "Placeholder QR yêu cầu số tiền nguyên dương." });

        var payment = order.Payment ?? new Payment
        {
            OrderID = order.OrderID,
            CreatedAt = DateTime.UtcNow
        };
        payment.Amount = amount;
        payment.PaymentMethod = "QRPlaceholder";
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

    private static decimal CalculateDiscount(decimal subtotal, string? type, decimal value) =>
        type?.ToLowerInvariant() switch
        {
            "percent" => subtotal * Math.Clamp(value, 0, 100) / 100,
            "fixed" => Math.Clamp(value, 0, subtotal),
            _ => 0
        };

    public sealed record CreateQrIntentRequest(int TableId, string? DiscountType, decimal DiscountValue);
}
