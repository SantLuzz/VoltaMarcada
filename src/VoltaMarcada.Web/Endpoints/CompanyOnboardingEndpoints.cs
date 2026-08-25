using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Application.CompanyOnboarding;
using VoltaMarcada.Infrastructure.Identity;
using VoltaMarcada.Web.Errors;
using VoltaMarcada.Web.Routing;

namespace VoltaMarcada.Web.Endpoints;

public static class CompanyOnboardingEndpoints
{
    public static IEndpointRouteBuilder MapCompanyOnboardingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/onboarding/company", CompleteAsync)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> CompleteAsync(
        [FromForm] CompleteCompanyOnboardingRequest request,
        CompleteCompanyOnboardingUseCase useCase,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (user.IsInRole(PlatformRoles.PlatformAdministrator))
        {
            return Results.Redirect(LocalReturnUrl.Normalize(request.ReturnUrl, "/"));
        }

        await using var logoStream = request.Logo?.OpenReadStream();
        var logo = logoStream is null || request.Logo is null
            ? null
            : new FileUpload(
                logoStream,
                request.Logo.FileName,
                request.Logo.ContentType,
                request.Logo.Length);

        var command = new CompleteCompanyOnboardingCommand(
            request.LegalName,
            request.TradeName,
            request.Document,
            request.WhatsApp,
            request.PostalCode,
            request.Address,
            request.StateId,
            request.CityId,
            logo);

        var result = await useCase.ExecuteAsync(command, cancellationToken);
        if (result.IsSuccess || result.Error?.Code == CompanyOnboardingErrors.AlreadyOnboarded.Code)
        {
            return Results.Redirect(LocalReturnUrl.Normalize(request.ReturnUrl, "/"));
        }

        var target = result.Error?.Code == CompanyOnboardingErrors.Unauthenticated.Code
            ? "/account/login"
            : "/onboarding/company";
        return RedirectWithError(
            target,
            OperationErrorMessages.GetMessage(result.Error?.Code),
            request.ReturnUrl);
    }

    private static IResult RedirectWithError(string path, string error, string? returnUrl) =>
        Results.Redirect(path + QueryString.Create("error", error)
            .Add("returnUrl", LocalReturnUrl.Normalize(returnUrl, "/")));

    public sealed record CompleteCompanyOnboardingRequest(
        string LegalName,
        string TradeName,
        string Document,
        string WhatsApp,
        string PostalCode,
        string Address,
        Guid StateId,
        Guid CityId,
        IFormFile? Logo = null,
        string? ReturnUrl = null);
}
