using Microsoft.AspNetCore.Mvc;
using ServiceSync.Application.CompanyOnboarding;

namespace ServiceSync.Web.Endpoints;

public static class CompanyOnboardingEndpoints
{
    public static IEndpointRouteBuilder MapCompanyOnboardingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/onboarding/company", CompleteAsync);
        return endpoints;
    }

    private static async Task<IResult> CompleteAsync(
        [FromForm] CompleteCompanyOnboardingRequest request,
        CompleteCompanyOnboardingUseCase useCase,
        CancellationToken cancellationToken)
    {
        var command = new CompleteCompanyOnboardingCommand(
            request.LegalName,
            request.TradeName,
            request.Document,
            request.Phone,
            request.WhatsApp,
            request.City,
            request.State,
            request.LogoKey);

        var result = await useCase.ExecuteAsync(command, cancellationToken);
        return result.Code switch
        {
            CompleteCompanyOnboardingResultCode.Success => Results.Redirect("/dashboard"),
            CompleteCompanyOnboardingResultCode.Unauthenticated => RedirectWithError(
                "/account/login",
                "Faça login para cadastrar sua empresa."),
            CompleteCompanyOnboardingResultCode.AlreadyOnboarded => RedirectWithMessage(
                "/dashboard",
                "Sua empresa já foi cadastrada."),
            CompleteCompanyOnboardingResultCode.InvalidInput => RedirectWithError(
                "/onboarding/company",
                "Revise os dados da empresa e tente novamente."),
            CompleteCompanyOnboardingResultCode.DocumentAlreadyExists => RedirectWithError(
                "/onboarding/company",
                "Este CPF ou CNPJ já está cadastrado."),
            CompleteCompanyOnboardingResultCode.SlugAlreadyExists => RedirectWithError(
                "/onboarding/company",
                "Não foi possível gerar um endereço público para a empresa. Tente novamente."),
            _ => throw new InvalidOperationException($"Unsupported company onboarding result code: {result.Code}.")
        };
    }

    private static IResult RedirectWithError(string path, string error) =>
        Results.Redirect(path + QueryString.Create("error", error));

    private static IResult RedirectWithMessage(string path, string message) =>
        Results.Redirect(path + QueryString.Create("message", message));

    public sealed record CompleteCompanyOnboardingRequest(
        string LegalName,
        string TradeName,
        string Document,
        string Phone,
        string WhatsApp,
        string City,
        string State,
        string? LogoKey);
}
