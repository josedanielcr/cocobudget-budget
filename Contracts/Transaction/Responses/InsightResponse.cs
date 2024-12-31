using web_api.Enums;

namespace web_api.Contracts.Transaction.Responses;

public class InsightResponse
{
    public TransactionInsightType Type { get; set; }
    public string Message { get; set; } = string.Empty;
}