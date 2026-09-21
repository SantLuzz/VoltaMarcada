using VoltaMarcada.Application.Support;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Errors;

namespace VoltaMarcada.Web.Endpoints;

public static class SupportEndpoints
{
    private const long MaximumAttachmentBytes = 5L * 1024 * 1024;

    public static IEndpointRouteBuilder MapSupportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/support/requests", SendAsync)
            .RequireAuthorization(AuthorizationPolicies.CompanyMember)
            .RequireRateLimiting("support-requests");

        return endpoints;
    }

    private static async Task<IResult> SendAsync(HttpRequest request, CreateSupportTicketUseCase useCase, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType) return Results.BadRequest();

        var form = await request.ReadFormAsync(cancellationToken);
        var attachments = new List<SupportAttachmentInput>(form.Files.Count);
        foreach (var file in form.Files)
        {
            if (file.Length > MaximumAttachmentBytes) return Results.BadRequest();
            await using var stream = file.OpenReadStream();
            using var content = new MemoryStream();
            await stream.CopyToAsync(content, cancellationToken);
            attachments.Add(new SupportAttachmentInput(file.FileName, file.ContentType, content.ToArray()));
        }

        var result = await useCase.ExecuteAsync(new SendSupportRequestCommand(
            form["subject"].ToString(),
            form["message"].ToString(),
            attachments), cancellationToken);

        return result.ToHttpResult(value => Results.Ok(new { ticketNumber = value.Number }));
    }
}
