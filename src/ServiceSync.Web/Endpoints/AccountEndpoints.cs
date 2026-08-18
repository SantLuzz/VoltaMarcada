using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceSync.Infrastructure.Identity;
using ServiceSync.Infrastructure.Persistence;

namespace ServiceSync.Web.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/register", RegisterAsync);
        endpoints.MapPost("/account/login", LoginAsync);
        endpoints.MapPost("/account/logout", LogoutAsync);

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        [FromForm] RegisterAccountRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        if (!IsValidEmail(request.Email) || string.IsNullOrWhiteSpace(request.Password) || request.Password != request.ConfirmPassword)
        {
            return RedirectWithError("/account/register", "Informe um e-mail válido e senhas iguais.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim()
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return RedirectWithError("/account/register", "Não foi possível criar a conta. Verifique os dados informados.");
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Redirect("/onboarding/company");
    }

    private static async Task<IResult> LoginAsync(
        [FromForm] LoginAccountRequest request,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return RedirectWithError("/account/login", "E-mail ou senha inválidos.");
        }

        var email = request.Email.Trim();
        var signInResult = await signInManager.PasswordSignInAsync(
            email,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            return RedirectWithError("/account/login", "E-mail ou senha inválidos.");
        }

        var user = await signInManager.UserManager.FindByNameAsync(email);
        if (user is null)
        {
            await signInManager.SignOutAsync();
            return RedirectWithError("/account/login", "E-mail ou senha inválidos.");
        }

        var hasMembership = await dbContext.CompanyUsers
            .AsNoTracking()
            .AnyAsync(companyUser => companyUser.UserId == user.Id, cancellationToken);

        return Results.Redirect(hasMembership ? "/dashboard" : "/onboarding/company");
    }

    private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.Redirect("/account/login");
    }

    private static bool IsValidEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && new EmailAddressAttribute().IsValid(email);

    private static IResult RedirectWithError(string path, string error) =>
        Results.Redirect(path + QueryString.Create("error", error));

    public sealed record RegisterAccountRequest(string Email, string Password, string ConfirmPassword);

    public sealed record LoginAccountRequest(string Email, string Password);
}
