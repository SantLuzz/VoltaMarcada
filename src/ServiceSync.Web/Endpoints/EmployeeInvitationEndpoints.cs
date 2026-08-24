using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceSync.Application.EmployeeInvitations;
using ServiceSync.Infrastructure.Identity;
using ServiceSync.Web.Errors;

namespace ServiceSync.Web.Endpoints;

public static class EmployeeInvitationEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeInvitationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/convites/funcionarios/{token}", AcceptAsync)
            .AllowAnonymous();
        return endpoints;
    }

    private static async Task<IResult> AcceptAsync(
        string token,
        [FromForm] AcceptEmployeeInvitationRequest request,
        AcceptEmployeeInvitationUseCase useCase,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(token, request.Password, request.ConfirmPassword, cancellationToken);
        if (!result.IsSuccess || result.Value?.UserId is not { } userId)
        {
            return RedirectWithError(token, GetAcceptanceError(result.Error?.Code));
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Redirect("/");
    }

    private static IResult RedirectWithError(string token, string error) =>
        Results.Redirect("/convites/funcionarios/" + Uri.EscapeDataString(token) + QueryString.Create("error", error));

    private static string GetAcceptanceError(string? code) => code switch
    {
        "EmployeeInvitation.InvalidInput" => "Informe senhas iguais que atendam aos critérios de segurança.",
        "EmployeeInvitation.EmailAlreadyRegistered" => "Não foi possível concluir este cadastro. Entre em contato com a empresa.",
        _ => "Este link é inválido, expirou ou já foi utilizado."
    };

    public sealed record AcceptEmployeeInvitationRequest(string Password, string ConfirmPassword);
}
