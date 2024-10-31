using web_api.Enums;

namespace web_api.Extensions;

public static class PeriodExtensions
{
    public static int GetNumberOfDays(DateTime startDate, int dayLength, PeriodLength length)
    {
        var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
        return length switch
        {
            PeriodLength.Weekly => 7,
            PeriodLength.BiWeekly => 14,
            PeriodLength.Monthly => daysInMonth,
            PeriodLength.Custom => dayLength,
            _ => daysInMonth
        };
    }
}