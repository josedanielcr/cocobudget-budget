using web_api.Entities;
using web_api.Enums;

namespace web_api.Contracts.Period.Responses;

public class PeriodResponse : BaseEntity
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PeriodLength Length { get; set; }
    public int DayLength { get; set; }
    public Guid UserId { get; set; }
    public float AmountSpent { get; set; }
    public float BudgetAmount { get; set; }
    
    public PeriodResponse(Guid id, DateTime startDate, DateTime endDate, PeriodLength length, int dayLength, Guid userId, float amountSpent, float budgetAmount)
    {
        Id = id;
        StartDate = startDate;
        EndDate = endDate;
        Length = length;
        DayLength = dayLength;
        UserId = userId;
        AmountSpent = amountSpent;
        BudgetAmount = budgetAmount;
    }
}