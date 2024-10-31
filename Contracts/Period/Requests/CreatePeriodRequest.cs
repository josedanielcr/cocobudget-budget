using web_api.Enums;

namespace web_api.Contracts.Period.Requests;

public class CreatePeriodRequest
{
    public required DateTime StartDate { get; set; }
    public required PeriodLength Length { get; set; }
    public int DayLength { get; set; }
    public required Guid UserId { get; set; }
}