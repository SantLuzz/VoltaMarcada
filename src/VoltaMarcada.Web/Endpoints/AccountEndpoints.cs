using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Infrastructure.Identity;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Web.Routing;
using VoltaMarcada.Domain.ValueObjects;
using VoltaMarcada.Application.Abstractions.Communication;
using VoltaMarcada.Application.Abstractions.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Security.Claims;

namespace VoltaMarcada.Web.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/register", RegisterAsync);
        endpoints.MapPost("/account/login", LoginAsync);
        endpoints.MapPost("/account/forgot-password", ForgotPasswordAsync);
        endpoints.MapPost("/account/reset-password", ResetPasswordAsync);
        endpoints.MapPost("/account/change-password", ChangePasswordAsync)
            .RequireAuthorization();
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
            lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return RedirectWithError("/account/login", "Conta bloqueada por excesso de tentativas. Tente novamente mais tarde.", request.ReturnUrl);
        }

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

        if (isPlatformAdministrator)
        {
            return Results.Redirect("/plataforma");
        }

        return hasMembership
            ? Results.Redirect(LocalReturnUrl.Normalize(request.ReturnUrl, "/"))
            : RedirectToOnboarding(request.ReturnUrl);
    }

    private static async Task<IResult> ForgotPasswordAsync(
        [FromForm] ForgotPasswordRequest request,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IApplicationUrlBuilder applicationUrlBuilder,
        CancellationToken cancellationToken)
    {
        const string confirmation = "Se o e-mail estiver cadastrado, você receberá instruções para redefinir sua senha.";
        if (!IsValidEmail(request.Email)) return Results.Redirect("/account/forgot-password" + QueryString.Create("message", confirmation));

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetUrl = new Uri(applicationUrlBuilder.BuildApplicationUrl($"account/reset-password?userId={user.Id}&token={Uri.EscapeDataString(encodedToken)}"));
            var email = AccountPasswordResetEmail.Create(user.FullName ?? user.Email!, resetUrl);
            await emailSender.SendAsync(new EmailMessage(user.Email!, email.Subject, email.HtmlBody), cancellationToken);
        }

        return Results.Redirect("/account/forgot-password" + QueryString.Create("message", confirmation));
    }

    private static async Task<IResult> ResetPasswordAsync([FromForm] ResetPasswordRequest request, UserManager<ApplicationUser> userManager)
    {
        if (!Guid.TryParse(request.UserId, out var userId) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password) || request.Password != request.ConfirmPassword)
            return Results.Redirect("/account/reset-password" + QueryString.Create("error", "Não foi possível redefinir a senha."));

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Results.Redirect("/account/reset-password" + QueryString.Create("error", "Não foi possível redefinir a senha."));
        string token;
        try { token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token)); }
        catch (FormatException) { return Results.Redirect("/account/reset-password" + QueryString.Create("error", "Não foi possível redefinir a senha.")); }
        var result = await userManager.ResetPasswordAsync(user, token, request.Password);
        return result.Succeeded
            ? Results.Redirect("/account/login" + QueryString.Create("message", "Senha redefinida. Entre com a nova senha."))
            : Results.Redirect("/account/reset-password" + QueryString.Create("error", "Não foi possível redefinir a senha."));
    }

    private static async Task<IResult> ChangePasswordAsync([FromForm] ChangePasswordRequest request, ClaimsPrincipal principal, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null) return Results.Challenge();
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword != request.ConfirmPassword)
            return Results.Redirect("/account/change-password" + QueryString.Create("error", "Revise os dados da senha."));
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        return result.Succeeded
            ? Results.Redirect("/account/change-password" + QueryString.Create("message", "Senha alterada com sucesso."))
            : Results.Redirect("/account/change-password" + QueryString.Create("error", "Não foi possível alterar a senha. Verifique a senha atual e os requisitos."));
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

    public sealed record ForgotPasswordRequest(string Email);
    public sealed record ResetPasswordRequest(string UserId, string Token, string Password, string ConfirmPassword);
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
}
