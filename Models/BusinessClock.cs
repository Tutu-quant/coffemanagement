namespace Quản_lý_quán_cafe.Models;

/// <summary>
/// Business time for the café. Persisted timestamps remain UTC; reservation
/// wall-clock values are interpreted in Asia/Ho_Chi_Minh on every host OS.
/// </summary>
public static class BusinessClock
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static DateTime Now => FromUtc(DateTime.UtcNow);
    public static DateTime Today => Now.Date;
    public static DateTime StartOfTodayUtc => ToUtc(Today);
    public static DateTime StartOfTomorrowUtc => ToUtc(Today.AddDays(1));

    public static DateTime FromUtc(DateTime value)
    {
        var utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);
    }

    public static DateTime ToUtc(DateTime businessLocal)
    {
        var local = DateTime.SpecifyKind(businessLocal, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, Zone);
    }

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        throw new InvalidOperationException("Vietnam business time zone is unavailable on this host.");
    }
}
