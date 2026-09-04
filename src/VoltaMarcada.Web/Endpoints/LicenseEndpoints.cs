using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Application.Licensing;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Errors;

namespace VoltaMarcada.Web.Endpoints;

public static class LicenseEndpoints
{
    public static IEndpointRouteBuilder MapLicenseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/license")
            .RequireAuthorization(AuthorizationPolicies.CompanyOwnerLicenseRenewal);

        group.MapGet("/renewal", async (
            GetLicenseRenewalUseCase useCase,
            CancellationToken cancellationToken) =>
            (await useCase.ExecuteAsync(cancellationToken)).ToHttpResult(Results.Ok));

        group.MapPost("/payments", SubmitPaymentAsync);

        group.MapGet("/payments/{paymentId:guid}/proof", async (
            Guid paymentId,
            GetLicensePaymentProofUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var proof = await useCase.ExecuteAsync(paymentId, cancellationToken);
            return proof is null
                ? Results.NotFound()
                : Results.File(proof.Content, proof.ContentType);
        });

        return endpoints;
    }

    private static async Task<IResult> SubmitPaymentAsync(
        IFormFile proof,
        SubmitLicensePaymentUseCase useCase,
        CancellationToken cancellationToken)
    {
        await using var content = proof.OpenReadStream();
        var result = await useCase.ExecuteAsync(
            new LicensePaymentProofUpload(
                new FileUpload(content, proof.FileName, proof.ContentType, proof.Length)),
            cancellationToken);

        return result.ToHttpResult(payment => Results.Created(
            $"/license/payments/{payment.Id}/proof",
            payment));
    }
}
