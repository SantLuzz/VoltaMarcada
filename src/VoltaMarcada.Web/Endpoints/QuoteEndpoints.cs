using Microsoft.AspNetCore.RateLimiting;
using VoltaMarcada.Application.Abstractions.Documents;
using VoltaMarcada.Application.Quotes;
using VoltaMarcada.Web.Authorization;

namespace VoltaMarcada.Web.Endpoints;

public static class QuoteEndpoints
{
    public static IEndpointRouteBuilder MapQuoteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/quotes")
            .RequireAuthorization(AuthorizationPolicies.CompanyMember)
            .MapGet("/{quoteId:guid}/pdf", async (Guid quoteId, GetQuotePdfUseCase useCase, CancellationToken cancellationToken) =>
                ToPdfResult(await useCase.ExecuteAsync(quoteId, cancellationToken)));

        endpoints.MapGet("/q/{token}/pdf", async (string token, GetPublicQuotePdfUseCase useCase, CancellationToken cancellationToken) =>
                ToPdfResult(await useCase.ExecuteAsync(token, cancellationToken)))
            .RequireRateLimiting("public-booking");

        return endpoints;
    }

    internal static IResult ToPdfResult(QuotePdfFile? file) => file is null
        ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Quote unavailable")
        : Results.File(file.Content, "application/pdf", file.FileName);
}
