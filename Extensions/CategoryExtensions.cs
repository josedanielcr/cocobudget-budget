namespace web_api.Extensions;

public static class CategoryExtensions
{
    public static decimal CalculateFinalDateTargetAmount(DateTime finalDate, decimal targetAmount, int periodDayLength)
    {
        if (periodDayLength <= 0)
        {
            throw new ArgumentException("Period day length must be greater than zero.", nameof(periodDayLength));
        }
        var now = DateTime.Now;
        if (finalDate <= now)
        {
            throw new ArgumentException("Final date must be in the future.", nameof(finalDate));
        }
        var totalDays = (int)(finalDate - now).TotalDays;
        var amountOfPeriods = totalDays / periodDayLength;
        if (amountOfPeriods <= 0)
        {
            throw new InvalidOperationException("Calculated amount of periods must be greater than zero.");
        }
        return Math.Round(targetAmount / amountOfPeriods, 2);
    }
}