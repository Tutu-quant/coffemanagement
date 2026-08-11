namespace Quản_lý_quán_cafe.Models;

public static class ReservationPolicy
{
    public const int DurationMinutes = 120;
    public const int HoldBeforeMinutes = 30;

    public static bool IsActiveStatus(string? status) =>
        status is not ("Cancelled" or "Completed");

    public static bool IsHoldingStatus(string? status) =>
        status is not ("Cancelled" or "Completed" or "CheckedIn");

    public static bool IsBlockingNow(DateTime reservationDate, DateTime now) =>
        IsWithinBlockingWindow(reservationDate, now.AddMinutes(HoldBeforeMinutes), now);

    public static bool IsWithinBlockingWindow(DateTime reservationDate, DateTime latestStart, DateTime now) =>
        reservationDate <= latestStart && reservationDate.AddMinutes(DurationMinutes) > now;
}
