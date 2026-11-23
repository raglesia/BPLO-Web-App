namespace BusinessPermitLicensingSystem.Web;

public static class PhilippineBusinessClock
{
    private static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

    public static DateTime Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, BusinessTimeZone).DateTime;
    public static DateTime Today => Now.Date;
}
