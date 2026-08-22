using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceSync.Infrastructure.Identity;
using ServiceSync.Infrastructure.Persistence;
using ServiceSync.Web.Routing;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Web.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/register", RegisterAsync);
        endpoints.MapPost("/account/login", LoginAsync);
        endpoints.MapPost("/account/logout", LogoutAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        [FromForm] RegisterAccountRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 200)
        {
            return RedirectWithFieldError("fullName", "Informe seu nome completo.", request.ReturnUrl);
        }

        if (!BrazilianDocument.TryNormalize(request.Document, out var document) || document.Length != 11)
        {
            return RedirectWithFieldError("document", "Informe um CPF válido.", request.ReturnUrl);
        }

        if (string.IsNullOrWhiteSpace(request.Phone) || request.Phone.Trim().Length > 20)
        {
            return RedirectWithFieldError("phone", "Informe um telefone válido.", request.ReturnUrl);
        }

        if (string.IsNullOrWhiteSpace(request.Address) || request.Address.Trim().Length > 300)
        {
            return RedirectWithFieldError("address", "Informe seu endereço.", request.ReturnUrl);
        }

        if (!IsValidEmail(request.Email))
        {
            return RedirectWithFieldError("email", "Informe um e-mail válido.", request.ReturnUrl);
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return RedirectWithFieldError("password", "A senha precisa ter pelo menos 6 caracteres.", request.ReturnUrl);
        }

        if (request.Password != request.ConfirmPassword)
        {
            return RedirectWithFieldError("confirmPassword", "As senhas não coincidem.", request.ReturnUrl);
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName.Trim(),
            Document = document,
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim()
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code is "DuplicateUserName" or "DuplicateEmail"))
            {
                return RedirectWithFieldError("email", "Já existe uma conta cadastrada com este e-mail.", request.ReturnUrl);
            }

            if (result.Errors.Any(error => error.Code.StartsWith("Password", StringComparison.Ordinal)))
            {
                return RedirectWithFieldError("password", "A senha não atende aos requisitos de segurança.", request.ReturnUrl);
            }

            return RedirectWithError("/account/register", "Não foi possível criar a conta. Tente novamente.", request.ReturnUrl);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToOnboarding(request.ReturnUrl);
    }

    private static async Task<IResult> LoginAsync(
        [FromForm] LoginAccountRequest request,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return RedirectWithError("/account/login", "E-mail ou senha inválidos.", request.ReturnUrl);
        }

        var email = request.Email.Trim();
        var signInResult = await signInManager.PasswordSignInAsync(
            email,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            return RedirectWithError("/account/login", "E-mail ou senha inválidos.", request.ReturnUrl);
        }

        var user = await signInManager.UserManager.FindByNameAsync(email);
        if (user is null)
        {
            await signInManager.SignOutAsync();
            return RedirectWithError("/account/login", "E-mail ou senha inválidos.", request.ReturnUrl);
        }

        var hasMembership = await dbContext.CompanyUsers
            .AsNoTracking()
            .AnyAsync(companyUser => companyUser.UserId == user.Id, cancellationToken);

        var isPlatformAdministrator = await signInManager.UserManager.IsInRoleAsync(
            user,
            PlatformRoles.PlatformAdministrator);

        return hasMembership || isPlatformAdministrator
            ? Results.Redirect(LocalReturnUrl.Normalize(request.ReturnUrl, "/"))
            : RedirectToOnboarding(request.ReturnUrl);
    }

    private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.Redirect("/account/login");
    }

    private static bool IsValidEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && new EmailAddressAttribute().IsValid(email);

    private static IResult RedirectWithError(string path, string error, string? returnUrl) =>
        Results.Redirect(path + QueryString.Create("error", error)
            .Add("returnUrl", LocalReturnUrl.Normalize(returnUrl, "/")));

    private static IResult RedirectWithFieldError(string field, string error, string? returnUrl) =>
        Results.Redirect("/account/register" + QueryString.Create("field", field)
            .Add("fieldError", error)
            .Add("returnUrl", LocalReturnUrl.Normalize(returnUrl, "/")));

    private static IResult RedirectToOnboarding(string? returnUrl) =>
        Results.Redirect("/onboarding/company" + QueryString.Create(
            "returnUrl", LocalReturnUrl.Normalize(returnUrl, "/")));

    public sealed record RegisterAccountRequest(
        string FullName,
        string Document,
        string Phone,
        string Address,
        string Email,
        string Password,
        string ConfirmPassword,
        string? ReturnUrl);

    public sealed record LoginAccountRequest(string Email, string Password, string? ReturnUrl);
}
