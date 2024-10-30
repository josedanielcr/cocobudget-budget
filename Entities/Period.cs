using web_api.Enums;

namespace web_api.Entities;

public class Period : BaseEntity
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public  PeriodLength Length { get; set; }
    public int DayLength { get; set; }
    public Guid UserId { get; set; }
    public float AmountSpent { get; set; }
    public float BudgetAmount { get; set; }

    public Period( DateTime startDate, PeriodLength length, int dayLength, Guid userId)
    {
        StartDate = startDate;
        Length = length;
        UserId = userId;
        AmountSpent = 0;
        BudgetAmount = 0;
        DayLength = SetDayLength(dayLength, startDate);
        EndDate = startDate.AddDays(dayLength - 1);
    }
    
    private int SetDayLength(int dayLength, DateTime startDate)
    {
        var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
        return Length switch
        {
            PeriodLength.Weekly => 7,
            PeriodLength.BiWeekly => 14,
            PeriodLength.Monthly => daysInMonth,
            PeriodLength.Custom => dayLength,
            _ => daysInMonth
        };
    }
}