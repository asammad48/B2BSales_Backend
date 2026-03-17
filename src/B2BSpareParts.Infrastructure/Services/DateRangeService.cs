using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs;

namespace B2BSpareParts.Infrastructure.Services;

public class DateRangeService : IDateRangeService
{
    public (DateTime start, DateTime end) CalculateDateRange(RangeType rangeType, DateTime? startDate, DateTime? endDate)
    {
        DateTime now = DateTime.UtcNow;
        DateTime start, end;

        switch (rangeType)
        {
            case RangeType.Day:
                start = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
                end = start.AddDays(1).AddTicks(-1);
                break;
            case RangeType.Week:
                int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                start = now.AddDays(-1 * diff).Date;
                start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
                end = start.AddDays(7).AddTicks(-1);
                break;
            case RangeType.Month:
                start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                end = start.AddMonths(1).AddTicks(-1);
                break;
            case RangeType.Custom:
                if (!startDate.HasValue || !endDate.HasValue)
                    throw new Exception("Start and end dates are required for custom range");
                start = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                end = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(rangeType), rangeType, null);
        }

        return (start, end);
    }
}
