namespace Quản_lý_quán_cafe.Services.Interfaces;

public static class LoyaltyRules
{
    public const int VndPerEarnedPoint = 10_000;
    public const int VndPerRedeemedPoint = 100;
    public const int MaxGiftPoints = 1_000_000;
}

public static class LoyaltyDiscountModes
{
    public const string None = "None";
    public const string Points = "Points";
    public const string Voucher = "Voucher";
}

public sealed class LoyaltyRuleException(string message, int statusCode = 400) : InvalidOperationException(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed record LoyaltyAccountDto(
    int CustomerId,
    string Name,
    string Username,
    string Phone,
    int AvailablePoints,
    int PointsUsed,
    decimal DiscountAmount);

public sealed record LoyaltyQuoteDto(
    int OrderId,
    decimal SubtotalAmount,
    decimal PointDiscountAmount,
    decimal VoucherDiscountAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    string Mode,
    string? VoucherCode,
    int EarnedPoints,
    IReadOnlyList<LoyaltyAccountDto> Accounts);

public sealed record PointHistoryDto(
    int PointHistoryId,
    int Points,
    int BalanceAfter,
    string TransactionType,
    string? Description,
    int? OrderId,
    DateTime TransactionDate);

public sealed record CustomerLoyaltySummaryDto(
    int CustomerId,
    int RewardPoints,
    IReadOnlyList<PointHistoryDto> History);

public interface ILoyaltyService
{
    Task<IReadOnlyList<LoyaltyAccountDto>> SearchAccountsAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<CustomerLoyaltySummaryDto> GetCustomerSummaryAsync(
        int customerId,
        int historyLimit = 50,
        CancellationToken cancellationToken = default);

    Task<LoyaltyQuoteDto> GetOrderQuoteAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<LoyaltyQuoteDto> ApplyPointsAsync(
        int orderId,
        IReadOnlyCollection<int> customerIds,
        int actorUserId,
        int? ownerCustomerId = null,
        CancellationToken cancellationToken = default);

    Task<LoyaltyQuoteDto> ApplyVoucherAsync(
        int orderId,
        string code,
        int actorUserId,
        int? ownerCustomerId = null,
        int? accountCustomerId = null,
        CancellationToken cancellationToken = default);

    Task<LoyaltyQuoteDto> ClearDiscountAsync(
        int orderId,
        int actorUserId,
        int? ownerCustomerId = null,
        CancellationToken cancellationToken = default);

    Task<CustomerLoyaltySummaryDto> GiftPointsAsync(
        int customerId,
        int points,
        string? reason,
        int actorUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates the authoritative total, consumes the selected point balances and
    /// awards points for the pre-discount subtotal. The caller must hold the application
    /// mutation lock and save the DbContext together with the completed payment.
    /// </summary>
    Task<LoyaltyQuoteDto> PrepareCheckoutAsync(
        int orderId,
        int actorUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a stale discount when order lines are changed. The caller owns the
    /// mutation lock and SaveChanges call.
    /// </summary>
    Task ResetDiscountForChangedOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default);
}
