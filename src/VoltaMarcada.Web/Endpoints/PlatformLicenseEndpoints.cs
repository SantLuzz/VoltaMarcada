using VoltaMarcada.Application.Licensing;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Infrastructure.Licensing;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Errors;

namespace VoltaMarcada.Web.Endpoints;

public static class PlatformLicenseEndpoints
{
    public static IEndpointRouteBuilder MapPlatformLicenseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/platform/licenses/payments")
            .RequireAuthorization(AuthorizationPolicies.PlatformAdministrator);

        group.MapGet("/", async (GetPlatformLicensePaymentsUseCase useCase, CancellationToken cancellationToken) =>
            Results.Ok(await useCase.ExecuteAsync(cancellationToken)));

        group.MapPost("/send-overdue-notifications", async (OverdueLicenseNotificationService service, CancellationToken cancellationToken) =>
        {
            await service.SendOverdueNotificationsAsync(cancellationToken);
            return Results.NoContent();
        });

        group.MapGet("/{paymentId:guid}/proof", async (
            Guid paymentId,
            GetPlatformLicensePaymentProofUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var proof = await useCase.ExecuteAsync(paymentId, cancellationToken);
            return proof is null ? Results.NotFound() : Results.File(proof.Content, proof.ContentType);
        });
        group.MapPost("/manual-release", async (
            ManualLicenseReleaseCommand command,
            ReleaseCompanyLicenseManuallyUseCase useCase,
            CancellationToken cancellationToken) =>
            (await useCase.ExecuteAsync(command, cancellationToken)).ToHttpResult(() => Results.NoContent()));
        group.MapPost("/{paymentId:guid}/review", async (
            Guid paymentId,
            LicensePaymentReviewCommand command,
            ReviewLicensePaymentUseCase useCase,
            CancellationToken cancellationToken) =>
            (await useCase.ExecuteAsync(paymentId, command, cancellationToken)).ToHttpResult(() => Results.NoContent()));

        return endpoints;
    }
}
