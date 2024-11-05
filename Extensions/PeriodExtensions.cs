using Microsoft.EntityFrameworkCore;
using web_api.Database;
using web_api.Entities;
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
    
    public static async Task<Period?> GetUserActivePeriodAsync(this ApplicationDbContext dbContext, Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Periods
            .Where(p => p.UserId == userId && p.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }
}