namespace DistriFlow.Application.Orders.Queries;

internal static class OrderCalendar
{
    internal static DateTime StartOfDayUtc(DateOnly date, TimeZoneInfo timeZone)
    {
        var midnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        // Some zones advance their clocks at midnight. Use the first valid instant of that day.
        while (timeZone.IsInvalidTime(midnight))
            midnight = midnight.AddMinutes(1);

        if (timeZone.IsAmbiguousTime(midnight))
            return new DateTimeOffset(midnight, timeZone.GetAmbiguousTimeOffsets(midnight).Max()).UtcDateTime;

        return TimeZoneInfo.ConvertTimeToUtc(midnight, timeZone);
    }
}
