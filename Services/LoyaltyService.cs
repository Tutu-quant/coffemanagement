using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.Enums;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Services;

public sealed class LoyaltyService(
    ApplicationDbContext context,
    IApplicationMutationCoordinator mutationCoordinator) : ILoyaltyService
{
    public async Task<IReadOnlyList<LoyaltyAccountDto>> SearchAccountsAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        query = query.Trim();
        if (query.Length < 2) return [];
        limit = Math.Clamp(limit, 1, 20);

        return await context.Customers.AsNoTracking()
            .Where(customer => !customer.IsDeleted && customer.IsActive && customer.User != null
                && !customer.User.IsDeleted && customer.User.IsActive
                && (customer.CustomerName.Contains(query)
                    || (customer.Phone != null && customer.Phone.Contains(query))
                    || customer.User.Username.Contains(query)))
            .OrderBy(customer => customer.CustomerName)
            .ThenBy(customer => customer.CustomerID)
            .Take(limit)
            .Select(customer => new LoyaltyAccountDto(
                customer.CustomerID,
                customer.CustomerName,
                customer.User!.Username,
                customer.Phone ?? string.Empty,
                customer.RewardPoints,
                0,
                0))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerLoyaltySummaryDto> GetCustomerSummaryAsync(
        int customerId,
        int historyLimit = 50,
        CancellationToken cancellationToken = default)
    {
        historyLimit = Math.Clamp(historyLimit, 1, 200);
        var customer = await context.Customers.AsNoTracking()
            .Where(item => item.CustomerID == customerId && !item.IsDeleted)
            .Select(item => new { item.CustomerID, item.RewardPoints })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new LoyaltyRuleException("Không tìm thấy tài khoản khách hàng.", 404);

        var history = await context.PointHistories.AsNoTracking()
            .Where(item => item.CustomerID == customerId && !item.IsDeleted)
            .OrderByDescending(item => item.TransactionDate)
            .ThenByDescending(item => item.PointHistoryID)
            .Take(historyLimit)
            .Select(item => new PointHistoryDto(
                item.PointHistoryID,
                item.Points,
                item.BalanceAfter,
                item.TransactionType,
                item.Description,
                item.OrderID,
                item.TransactionDate))
            .ToListAsync(cancellationToken);

        return new CustomerLoyaltySummaryDto(customer.CustomerID, customer.RewardPoints, history);
    }

    public async Task<LoyaltyQuoteDto> GetOrderQuoteAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderAsync(orderId, tracking: false, cancellationToken);
        EnsureOrderExists(order);

        if (order!.Payment?.PaymentStatus == PaymentStatusConstants.Completed
            || order.OrderStatus == OrderStatusConstants.Completed)
        {
            return BuildCompletedQuote(order);
        }

        EnsureMutable(order);
        return CalculateQuote(order, validateVoucher: true).Quote;
    }

    public async Task<LoyaltyQuoteDto> ApplyPointsAsync(
        int orderId,
        IReadOnlyCollection<int> customerIds,
        int actorUserId,
        int? ownerCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        await using var mutationLock = await mutationCoordinator.EnterAsync(cancellationToken);
        var order = await LoadOrderAsync(orderId, tracking: true, cancellationToken);
        EnsureOrderExists(order);
        var mutableOrder = order!;
        EnsureMutable(mutableOrder);
        EnsureOwner(mutableOrder, ownerCustomerId);

        var orderedIds = customerIds.Where(id => id > 0).Distinct().Take(10).ToArray();
        if (orderedIds.Length == 0)
            throw new LoyaltyRuleException("Hãy chọn ít nhất một tài khoản điểm.");
        if (ownerCustomerId.HasValue
            && (orderedIds.Length != 1 || orderedIds[0] != ownerCustomerId.Value))
            throw new LoyaltyRuleException("Bạn chỉ có thể sử dụng điểm của chính mình.", 403);

        var customers = await context.Customers
            .Include(customer => customer.User)
            .Where(customer => orderedIds.Contains(customer.CustomerID)
                && !customer.IsDeleted && customer.IsActive
                && customer.User != null && !customer.User.IsDeleted && customer.User.IsActive)
            .ToDictionaryAsync(customer => customer.CustomerID, cancellationToken);
        if (customers.Count != orderedIds.Length)
            throw new LoyaltyRuleException("Một hoặc nhiều tài khoản điểm không còn hoạt động.");

        AssignLoyaltyCustomer(mutableOrder, customers[orderedIds[0]]);

        RemovePointSelections(mutableOrder);
        ClearVoucher(mutableOrder);

        var subtotal = GrossSubtotal(mutableOrder);
        var remaining = subtotal;
        var sequence = 0;
        foreach (var customerId in orderedIds)
        {
            var customer = customers[customerId];
            if (customer.RewardPoints <= 0 || remaining <= 0) continue;

            var needed = checked((int)decimal.Floor(remaining / LoyaltyRules.VndPerRedeemedPoint));
            var used = Math.Min(customer.RewardPoints, needed);
            var discount = Math.Min(remaining, used * (decimal)LoyaltyRules.VndPerRedeemedPoint);
            if (used <= 0 || discount <= 0) continue;

            mutableOrder.PointRedemptions.Add(new OrderPointRedemption
            {
                OrderID = mutableOrder.OrderID,
                CustomerID = customer.CustomerID,
                Customer = customer,
                PointsUsed = used,
                DiscountAmount = discount,
                Sequence = sequence++,
                CreatedAt = DateTime.UtcNow
            });
            remaining -= discount;
        }

        if (mutableOrder.PointRedemptions.Count == 0)
            throw new LoyaltyRuleException("Các tài khoản đã chọn chưa có điểm để áp dụng.");

        ApplyCalculatedTotals(mutableOrder, subtotal, 0, subtotal - remaining);
        mutableOrder.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return CalculateQuote(mutableOrder, validateVoucher: true).Quote;
    }

    public async Task<LoyaltyQuoteDto> ApplyVoucherAsync(
        int orderId,
        string code,
        int actorUserId,
        int? ownerCustomerId = null,
        int? accountCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        await using var mutationLock = await mutationCoordinator.EnterAsync(cancellationToken);
        var order = await LoadOrderAsync(orderId, tracking: true, cancellationToken);
        EnsureOrderExists(order);
        var mutableOrder = order!;
        EnsureMutable(mutableOrder);
        EnsureOwner(mutableOrder, ownerCustomerId);

        var primaryCustomerId = ownerCustomerId ?? accountCustomerId;
        if (ownerCustomerId.HasValue && accountCustomerId.HasValue
            && ownerCustomerId.Value != accountCustomerId.Value)
            throw new LoyaltyRuleException("Bạn chỉ có thể áp dụng voucher cho tài khoản của chính mình.", 403);
        if (primaryCustomerId.HasValue)
        {
            var primaryCustomer = await context.Customers
                .Include(customer => customer.User)
                .SingleOrDefaultAsync(customer =>
                customer.CustomerID == primaryCustomerId.Value && !customer.IsDeleted && customer.IsActive
                && customer.User != null && !customer.User.IsDeleted && customer.User.IsActive,
                cancellationToken);
            if (primaryCustomer is null)
                throw new LoyaltyRuleException("Tài khoản khách hàng không còn hoạt động.");
            if (mutableOrder.CustomerID.HasValue
                && mutableOrder.CustomerID != primaryCustomerId.Value
                && !mutableOrder.IsLoyaltyCustomerAssigned)
                throw new LoyaltyRuleException("Đơn đã gắn với một tài khoản khách hàng khác.", 409);
            AssignLoyaltyCustomer(mutableOrder, primaryCustomer);
        }

        var normalizedCode = NormalizeVoucherCode(code);
        var now = DateTime.UtcNow;
        var voucher = await context.Vouchers.FirstOrDefaultAsync(item =>
            item.Code == normalizedCode && !item.IsDeleted && item.IsActive
            && (!item.StartDate.HasValue || item.StartDate <= now)
            && (!item.EndDate.HasValue || item.EndDate >= now), cancellationToken);
        if (voucher is null)
            throw new LoyaltyRuleException("Voucher không tồn tại, đã hết hạn hoặc đang bị khóa.");

        RemovePointSelections(mutableOrder);
        mutableOrder.Voucher = voucher;
        mutableOrder.VoucherID = voucher.VoucherID;
        mutableOrder.VoucherCode = voucher.Code;
        var subtotal = GrossSubtotal(mutableOrder);
        var voucherDiscount = CalculateVoucherDiscount(subtotal, voucher);
        ApplyCalculatedTotals(mutableOrder, subtotal, voucherDiscount, 0);
        mutableOrder.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return CalculateQuote(mutableOrder, validateVoucher: true).Quote;
    }

    public async Task<LoyaltyQuoteDto> ClearDiscountAsync(
        int orderId,
        int actorUserId,
        int? ownerCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        await using var mutationLock = await mutationCoordinator.EnterAsync(cancellationToken);
        var order = await LoadOrderAsync(orderId, tracking: true, cancellationToken);
        EnsureOrderExists(order);
        var mutableOrder = order!;
        EnsureMutable(mutableOrder);
        EnsureOwner(mutableOrder, ownerCustomerId);

        RemovePointSelections(mutableOrder);
        ClearVoucher(mutableOrder);
        var subtotal = GrossSubtotal(mutableOrder);
        ApplyCalculatedTotals(mutableOrder, subtotal, 0, 0);
        mutableOrder.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return CalculateQuote(mutableOrder, validateVoucher: true).Quote;
    }

    public async Task<CustomerLoyaltySummaryDto> GiftPointsAsync(
        int customerId,
        int points,
        string? reason,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (points <= 0 || points > LoyaltyRules.MaxGiftPoints)
            throw new LoyaltyRuleException($"Số điểm tặng phải từ 1 đến {LoyaltyRules.MaxGiftPoints:N0}.");
        reason = string.IsNullOrWhiteSpace(reason) ? "Admin tặng điểm" : reason.Trim();
        if (reason.Length > 300)
            throw new LoyaltyRuleException("Lý do tặng điểm không được vượt quá 300 ký tự.");

        await using var mutationLock = await mutationCoordinator.EnterAsync(cancellationToken);
        var customer = await context.Customers
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.CustomerID == customerId && !item.IsDeleted, cancellationToken);
        if (customer?.User is null || !customer.IsActive || customer.User.IsDeleted || !customer.User.IsActive)
            throw new LoyaltyRuleException("Chỉ có thể tặng điểm cho tài khoản khách hàng đang hoạt động.");

        customer.RewardPoints = checked(customer.RewardPoints + points);
        customer.UpdatedAt = DateTime.UtcNow;
        context.PointHistories.Add(new PointHistory
        {
            CustomerID = customer.CustomerID,
            Points = points,
            BalanceAfter = customer.RewardPoints,
            TransactionType = "Grant",
            Description = reason,
            ActorUserID = actorUserId,
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new LoyaltyRuleException(
                "Số dư điểm vừa thay đổi ở giao dịch khác. Vui lòng tải lại và thử lại.",
                409);
        }
        return await GetCustomerSummaryAsync(customerId, 50, cancellationToken);
    }

    public async Task<LoyaltyQuoteDto> PrepareCheckoutAsync(
        int orderId,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderAsync(orderId, tracking: true, cancellationToken);
        EnsureOrderExists(order);
        var mutableOrder = order!;
        EnsureMutable(mutableOrder);

        var calculation = CalculateQuote(mutableOrder, validateVoucher: true);
        var activeAllocations = calculation.Allocations
            .Where(item => item.PointsUsed > 0 && item.DiscountAmount > 0)
            .ToDictionary(item => item.Redemption.OrderPointRedemptionID);
        foreach (var stale in mutableOrder.PointRedemptions
                     .Where(item => !activeAllocations.ContainsKey(item.OrderPointRedemptionID))
                     .ToList())
        {
            context.OrderPointRedemptions.Remove(stale);
            mutableOrder.PointRedemptions.Remove(stale);
        }

        foreach (var allocation in calculation.Allocations)
        {
            var redemption = allocation.Redemption;
            var customer = redemption.Customer
                ?? throw new LoyaltyRuleException("Tài khoản dùng điểm không còn tồn tại.", 409);
            var idempotencyKey = $"order:{mutableOrder.OrderID}:redeem:{customer.CustomerID}";
            if (redemption.PointHistoryID.HasValue
                || await context.PointHistories.AnyAsync(
                    history => history.IdempotencyKey == idempotencyKey, cancellationToken))
                throw new LoyaltyRuleException("Điểm của hóa đơn này đã được xử lý trước đó.", 409);
            if (customer.RewardPoints < allocation.PointsUsed)
                throw new LoyaltyRuleException("Số dư điểm vừa thay đổi. Vui lòng áp dụng lại điểm.", 409);

            customer.RewardPoints -= allocation.PointsUsed;
            customer.UpdatedAt = DateTime.UtcNow;
            redemption.PointsUsed = allocation.PointsUsed;
            redemption.DiscountAmount = allocation.DiscountAmount;
            var history = new PointHistory
            {
                CustomerID = customer.CustomerID,
                OrderID = mutableOrder.OrderID,
                Points = -allocation.PointsUsed,
                BalanceAfter = customer.RewardPoints,
                TransactionType = "Redeem",
                Description = $"Đổi điểm cho đơn #{mutableOrder.OrderID}",
                ActorUserID = actorUserId,
                IdempotencyKey = idempotencyKey,
                TransactionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            redemption.PointHistory = history;
            context.PointHistories.Add(history);
        }

        var earnedPoints = calculation.Quote.EarnedPoints;
        if (earnedPoints > 0 && mutableOrder.Customer is not null)
        {
            var earnKey = $"order:{mutableOrder.OrderID}:earn:{mutableOrder.Customer.CustomerID}";
            if (await context.PointHistories.AnyAsync(
                    history => history.IdempotencyKey == earnKey, cancellationToken))
                throw new LoyaltyRuleException("Điểm của hóa đơn này đã được cộng trước đó.", 409);

            mutableOrder.Customer.RewardPoints = checked(mutableOrder.Customer.RewardPoints + earnedPoints);
            mutableOrder.Customer.UpdatedAt = DateTime.UtcNow;
            context.PointHistories.Add(new PointHistory
            {
                CustomerID = mutableOrder.Customer.CustomerID,
                OrderID = mutableOrder.OrderID,
                Points = earnedPoints,
                BalanceAfter = mutableOrder.Customer.RewardPoints,
                TransactionType = "Earn",
                Description = $"Tích điểm từ đơn #{mutableOrder.OrderID}",
                ActorUserID = actorUserId,
                IdempotencyKey = earnKey,
                TransactionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }

        ApplyCalculatedTotals(
            mutableOrder,
            calculation.Quote.SubtotalAmount,
            calculation.Quote.VoucherDiscountAmount,
            calculation.Quote.PointDiscountAmount);
        mutableOrder.UpdatedAt = DateTime.UtcNow;
        return calculation.Quote;
    }

    public async Task ResetDiscountForChangedOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        if (orderId <= 0) return;
        var order = await LoadOrderAsync(orderId, tracking: true, cancellationToken);
        if (order is null || order.OrderStatus is OrderStatusConstants.Completed or OrderStatusConstants.Cancelled)
            return;

        RemovePointSelections(order);
        ClearVoucher(order);
        var subtotal = GrossSubtotal(order);
        ApplyCalculatedTotals(order, subtotal, 0, 0);
        order.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<Order?> LoadOrderAsync(
        int orderId,
        bool tracking,
        CancellationToken cancellationToken)
    {
        IQueryable<Order> query = context.Orders
            .Include(order => order.Payment)
            .Include(order => order.Voucher)
            .Include(order => order.Customer)!.ThenInclude(customer => customer!.User)
            .Include(order => order.OrderDetails.Where(detail => !detail.IsDeleted))
            .Include(order => order.PointRedemptions)
                .ThenInclude(redemption => redemption.Customer)!.ThenInclude(customer => customer!.User)
            .AsSplitQuery();
        if (!tracking) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(
            order => order.OrderID == orderId && !order.IsDeleted,
            cancellationToken);
    }

    private static void EnsureOrderExists(Order? order)
    {
        if (order is null)
            throw new LoyaltyRuleException("Không tìm thấy đơn hàng.", 404);
    }

    private static void EnsureMutable(Order order)
    {
        if (order.OrderStatus is OrderStatusConstants.Completed or OrderStatusConstants.Cancelled
            || order.Payment?.PaymentStatus == PaymentStatusConstants.Completed)
            throw new LoyaltyRuleException("Đơn hàng đã đóng, không thể thay đổi ưu đãi.", 409);
        if (!order.OrderDetails.Any(detail => !detail.IsDeleted))
            throw new LoyaltyRuleException("Đơn hàng chưa có món để áp dụng ưu đãi.", 409);
    }

    private static void EnsureOwner(Order order, int? ownerCustomerId)
    {
        if (ownerCustomerId.HasValue && order.CustomerID != ownerCustomerId.Value)
            throw new LoyaltyRuleException("Không tìm thấy đơn hàng.", 404);
    }

    private static string NormalizeVoucherCode(string code)
    {
        code = code.Trim().ToUpperInvariant();
        if (code.Length is < 2 or > 50 || code.Any(character => !char.IsLetterOrDigit(character)))
            throw new LoyaltyRuleException("Mã voucher không hợp lệ.");
        return code;
    }

    private static decimal GrossSubtotal(Order order) =>
        order.OrderDetails.Where(detail => !detail.IsDeleted).Sum(detail => detail.Subtotal);

    private static decimal CalculateVoucherDiscount(decimal subtotal, Voucher voucher)
    {
        var raw = voucher.DiscountType switch
        {
            Voucher.PercentDiscount => subtotal * voucher.DiscountValue / 100,
            Voucher.FixedDiscount => voucher.DiscountValue,
            _ => throw new LoyaltyRuleException("Cấu hình voucher không hợp lệ.", 409)
        };
        return Math.Clamp(decimal.Round(raw, 0, MidpointRounding.AwayFromZero), 0, subtotal);
    }

    private static void ApplyCalculatedTotals(
        Order order,
        decimal subtotal,
        decimal voucherDiscount,
        decimal pointDiscount)
    {
        voucherDiscount = Math.Clamp(voucherDiscount, 0, subtotal);
        pointDiscount = Math.Clamp(pointDiscount, 0, subtotal - voucherDiscount);
        order.SubtotalAmount = subtotal;
        order.VoucherDiscountAmount = voucherDiscount;
        order.PointDiscountAmount = pointDiscount;
        order.TotalAmount = Math.Max(0, subtotal - voucherDiscount - pointDiscount);
        if (order.Payment is { PaymentStatus: PaymentStatusConstants.Pending })
        {
            order.Payment.Amount = order.TotalAmount;
            order.Payment.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static void AssignLoyaltyCustomer(Order order, Customer selectedCustomer)
    {
        if (order.CustomerID.HasValue && !order.IsLoyaltyCustomerAssigned)
            return;

        order.CustomerID = selectedCustomer.CustomerID;
        order.Customer = selectedCustomer;
        order.IsLoyaltyCustomerAssigned = true;
    }

    private static void ClearVoucher(Order order)
    {
        order.Voucher = null;
        order.VoucherID = null;
        order.VoucherCode = null;
        order.VoucherDiscountAmount = 0;
    }

    private void RemovePointSelections(Order order)
    {
        var selections = order.PointRedemptions.ToList();
        if (selections.Any(item => item.PointHistoryID.HasValue))
            throw new LoyaltyRuleException("Điểm đã được quyết toán, không thể gỡ khỏi hóa đơn.", 409);
        context.OrderPointRedemptions.RemoveRange(selections);
        order.PointRedemptions.Clear();
        order.PointDiscountAmount = 0;
    }

    private CalculationResult CalculateQuote(Order order, bool validateVoucher)
    {
        var subtotal = GrossSubtotal(order);
        decimal voucherDiscount = 0;
        if (order.VoucherID.HasValue)
        {
            var voucher = order.Voucher;
            var now = DateTime.UtcNow;
            if (voucher is null || voucher.IsDeleted || !voucher.IsActive
                || (voucher.StartDate.HasValue && voucher.StartDate > now)
                || (voucher.EndDate.HasValue && voucher.EndDate < now))
            {
                if (validateVoucher)
                    throw new LoyaltyRuleException("Voucher của đơn không còn hiệu lực. Vui lòng chọn voucher khác.", 409);
            }
            else
            {
                voucherDiscount = CalculateVoucherDiscount(subtotal, voucher);
            }
        }

        var remaining = subtotal - voucherDiscount;
        var allocations = new List<PointAllocation>();
        if (!order.VoucherID.HasValue)
        {
            foreach (var redemption in order.PointRedemptions.OrderBy(item => item.Sequence))
            {
                if (remaining <= 0) break;
                var customer = redemption.Customer;
                if (customer?.User is null || customer.IsDeleted || !customer.IsActive
                    || customer.User.IsDeleted || !customer.User.IsActive)
                    throw new LoyaltyRuleException("Một tài khoản dùng điểm không còn hoạt động.", 409);

                var available = Math.Max(0, customer.RewardPoints);
                var needed = checked((int)decimal.Floor(remaining / LoyaltyRules.VndPerRedeemedPoint));
                var used = Math.Min(available, needed);
                var discount = Math.Min(remaining, used * (decimal)LoyaltyRules.VndPerRedeemedPoint);
                if (used <= 0 || discount <= 0) continue;
                allocations.Add(new PointAllocation(redemption, used, discount));
                remaining -= discount;
            }
        }

        var pointDiscount = subtotal - voucherDiscount - remaining;
        var accounts = allocations.Select(allocation => new LoyaltyAccountDto(
            allocation.Redemption.CustomerID,
            allocation.Redemption.Customer?.CustomerName ?? $"Tài khoản #{allocation.Redemption.CustomerID}",
            allocation.Redemption.Customer?.User?.Username ?? string.Empty,
            allocation.Redemption.Customer?.Phone ?? string.Empty,
            allocation.Redemption.Customer?.RewardPoints ?? 0,
            allocation.PointsUsed,
            allocation.DiscountAmount)).ToList();
        var mode = voucherDiscount > 0
            ? LoyaltyDiscountModes.Voucher
            : pointDiscount > 0 ? LoyaltyDiscountModes.Points : LoyaltyDiscountModes.None;
        var eligiblePrimary = order.Customer is { IsDeleted: false, IsActive: true, User: { IsDeleted: false, IsActive: true } };
        var earnedPoints = eligiblePrimary
            ? checked((int)decimal.Floor(subtotal / LoyaltyRules.VndPerEarnedPoint))
            : 0;
        var quote = new LoyaltyQuoteDto(
            order.OrderID,
            subtotal,
            pointDiscount,
            voucherDiscount,
            pointDiscount + voucherDiscount,
            Math.Max(0, remaining),
            mode,
            voucherDiscount > 0 ? order.Voucher?.Code ?? order.VoucherCode : null,
            earnedPoints,
            accounts);
        return new CalculationResult(quote, allocations);
    }

    private static LoyaltyQuoteDto BuildCompletedQuote(Order order)
    {
        var subtotal = order.SubtotalAmount > 0 ? order.SubtotalAmount : GrossSubtotal(order);
        var accounts = order.PointRedemptions.OrderBy(item => item.Sequence)
            .Select(item => new LoyaltyAccountDto(
                item.CustomerID,
                item.Customer?.CustomerName ?? $"Tài khoản #{item.CustomerID}",
                item.Customer?.User?.Username ?? string.Empty,
                item.Customer?.Phone ?? string.Empty,
                item.Customer?.RewardPoints ?? 0,
                item.PointsUsed,
                item.DiscountAmount))
            .ToList();
        var mode = order.VoucherDiscountAmount > 0
            ? LoyaltyDiscountModes.Voucher
            : order.PointDiscountAmount > 0 ? LoyaltyDiscountModes.Points : LoyaltyDiscountModes.None;
        var eligiblePrimary = order.Customer is { IsDeleted: false, IsActive: true, User: { IsDeleted: false, IsActive: true } };
        var earnedPoints = eligiblePrimary
            ? checked((int)decimal.Floor(subtotal / LoyaltyRules.VndPerEarnedPoint))
            : 0;
        return new LoyaltyQuoteDto(
            order.OrderID,
            subtotal,
            order.PointDiscountAmount,
            order.VoucherDiscountAmount,
            order.PointDiscountAmount + order.VoucherDiscountAmount,
            order.TotalAmount,
            mode,
            order.VoucherCode,
            earnedPoints,
            accounts);
    }

    private sealed record PointAllocation(
        OrderPointRedemption Redemption,
        int PointsUsed,
        decimal DiscountAmount);

    private sealed record CalculationResult(
        LoyaltyQuoteDto Quote,
        IReadOnlyList<PointAllocation> Allocations);
}
