using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceSync.Application.EmployeeInvitations;
using ServiceSync.Infrastructure.Identity;

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
        if (result.Code != EmployeeInvitationResultCode.Success || result.UserId is null)
        {
            return RedirectWithError(token, GetError(result.Code));
        }

        var user = await userManager.FindByIdAsync(result.UserId.Value.ToString());
        if (user is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Redirect("/");
    }

    private static IResult RedirectWithError(string token, string error) =>
        Results.Redirect("/convites/funcionarios/" + Uri.EscapeDataString(token) + QueryString.Create("error", error));

    private static string GetError(EmployeeInvitationResultCode code) => code switch
    {
        EmployeeInvitationResultCode.InvalidInput => "Informe senhas iguais que atendam aos critérios de segurança.",
        EmployeeInvitationResultCode.EmailAlreadyRegistered => "Não foi possível concluir este cadastro. Entre em contato com a empresa.",
        _ => "Este link é inválido, expirou ou já foi utilizado."
    };

    public sealed record AcceptEmployeeInvitationRequest(string Password, string ConfirmPassword);
}
