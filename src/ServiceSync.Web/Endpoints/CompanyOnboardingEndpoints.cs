using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ServiceSync.Application.Abstractions.Storage;
using ServiceSync.Application.CompanyOnboarding;
using ServiceSync.Infrastructure.Identity;
using ServiceSync.Web.Routing;

namespace ServiceSync.Web.Endpoints;

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
        return result.Code switch
        {
            CompleteCompanyOnboardingResultCode.Success => Results.Redirect(
                LocalReturnUrl.Normalize(request.ReturnUrl, "/")),
            CompleteCompanyOnboardingResultCode.Unauthenticated => RedirectWithError(
                "/account/login",
                "Faça login para cadastrar sua empresa.",
                request.ReturnUrl),
            CompleteCompanyOnboardingResultCode.AlreadyOnboarded => Results.Redirect(
                LocalReturnUrl.Normalize(request.ReturnUrl, "/")),
            CompleteCompanyOnboardingResultCode.InvalidInput => RedirectWithError(
                "/onboarding/company",
                "Revise os dados da empresa e tente novamente.",
                request.ReturnUrl),
            CompleteCompanyOnboardingResultCode.DocumentAlreadyExists => RedirectWithError(
                "/onboarding/company",
                "Este CPF ou CNPJ já está cadastrado.",
                request.ReturnUrl),
            CompleteCompanyOnboardingResultCode.SlugAlreadyExists => RedirectWithError(
                "/onboarding/company",
                "Não foi possível gerar um endereço público para a empresa. Tente novamente.",
                request.ReturnUrl),
            CompleteCompanyOnboardingResultCode.InvalidLocation => RedirectWithError(
                "/onboarding/company",
                "Selecione um estado e uma cidade válidos.",
                request.ReturnUrl),
            CompleteCompanyOnboardingResultCode.InvalidLogo => RedirectWithError(
                "/onboarding/company",
                "Envie uma logo PNG, JPEG ou WebP de até 5 MB.",
                request.ReturnUrl),
            _ => throw new InvalidOperationException($"Unsupported company onboarding result code: {result.Code}.")
        };
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
