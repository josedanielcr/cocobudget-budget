using Carter;
using MediatR;
using web_api.Contracts.External;
using web_api.Extensions;
using web_api.Shared;

namespace web_api.Features.Currency;

public class GetCurrencyCodes
{
    public class Query : IRequest<Result<CurrencyCodesResponse>> { }

    internal sealed class Handler(CurrencyExtension currencyExtension)
        : IRequestHandler<Query, Result<CurrencyCodesResponse>>
    {
        public async Task<Result<CurrencyCodesResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            return await currencyExtension.GetCurrencyCodesAsync();
        }
    }
}

public class GetCurrencyCodesEndpoint : CarterModule
{
    private const string RouteTag = "Currency";
    
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/currency/codes", async (ISender sender) =>
        {
            var query = new GetCurrencyCodes.Query {};
            var result = await sender.Send(query);
            return result.IsFailure ? Results.BadRequest(result.Error) : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}