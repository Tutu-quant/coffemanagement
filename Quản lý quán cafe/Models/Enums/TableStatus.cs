namespace Quản_lý_quán_cafe.Models.Enums;

public static class TableStatus
{
    public const string Available = "Available";
    public const string Reserved = "Reserved";
    public const string Occupied = "Occupied";
    public const string WaitingPayment = "WaitingPayment";
    public const string Maintenance = "Maintenance";

    public static readonly string[] All =
        [Available, Reserved, Occupied, WaitingPayment, Maintenance];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status) && All.Contains(status, StringComparer.OrdinalIgnoreCase);
}
