using MudBlazor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using VoltaMarcada.Application.CompanyOnboarding;
using VoltaMarcada.Application;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Routing;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Infrastructure;
using VoltaMarcada.Infrastructure.Identity;
using VoltaMarcada.Web.Components;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Endpoints;
using VoltaMarcada.Web.Errors;
using VoltaMarcada.Web.Identity;
using VoltaMarcada.Web.Operations;
using VoltaMarcada.Web.Configuration;
using VoltaMarcada.Web.Help;
using VoltaMarcada.Web.Formatting;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Application.PublicBooking;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Components.Server;
using VoltaMarcada.Application.Licensing;

var builder = WebApplication.CreateBuilder(args);

var brazilianCulture = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = brazilianCulture;
CultureInfo.DefaultThreadCurrentUICulture = brazilianCulture;
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(brazilianCulture);
    options.SupportedCultures = [brazilianCulture];
    options.SupportedUICultures = [brazilianCulture];
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });
builder.Services.Configure<CircuitOptions>(options =>
{
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
});
builder.Services.AddExceptionHandler<UnexpectedExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddOptions<SchedulingOptions>()
    .Bind(builder.Configuration.GetSection(SchedulingOptions.SectionName))
    .Validate(options => options.SlotIntervalMinutes > 0 && 60 % options.SlotIntervalMinutes == 0, "Slot interval must divide one hour.")
    .Validate(options => options.AvailabilitySearchDays > 0, "Availability search days must be positive.")
    .Validate(options => options.AvailabilityResultLimit > 0, "Availability result limit must be positive.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultTimeZoneId), "Default time zone is required.")
    .Validate(options => options.AllowedTimeZoneIds.Count > 0 && options.AllowedTimeZoneIds.Contains(options.DefaultTimeZoneId, StringComparer.Ordinal), "Allowed time zones must contain the default time zone.")
    .ValidateOnStart();
builder.Services.AddSingleton<ISchedulingConfiguration, SchedulingConfiguration>();
builder.Services.AddOptions<PublicBookingOptions>()
    .Bind(builder.Configuration.GetSection(PublicBookingOptions.SectionName))
    .Validate(options => options.MaintenanceLinkLifetimeDays is > 0 and <= 365, "Maintenance link lifetime must be between 1 and 365 days.")
    .ValidateOnStart();
builder.Services.AddSingleton<IPublicBookingConfiguration, PublicBookingConfiguration>();
builder.Services.AddOptions<SupportOptions>()
    .Bind(builder.Configuration.GetSection(SupportOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<ISupportConfiguration, SupportConfiguration>();
builder.Services.AddSingleton<IHelpGuideProvider, HelpGuideProvider>();
builder.Services.AddHttpClient();
builder.Services.AddOptions<CompanyLicenseTrialOptions>()
    .Bind(builder.Configuration.GetSection(CompanyLicenseTrialOptions.SectionName))
    .Validate(options => options.FreeTrialDays > 0, "Free trial days must be positive.")
    .ValidateOnStart();
builder.Services.AddSingleton<ICompanyLicenseTrialConfiguration, CompanyLicenseTrialConfiguration>();
builder.Services.AddOptions<LicenseBillingOptions>()
    .Bind(builder.Configuration.GetSection(LicenseBillingOptions.SectionName))
    .Validate(options => options.MonthlyPrice > 0, "The license monthly price must be positive.")
    .Validate(options => options.ManualReleaseDefaultValidityDays > 0, "The manual release validity must be positive.")
    .Validate(options => options.MaximumProofSizeBytes > 0, "The maximum proof size must be positive.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.PixKey), "The PIX key is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.BeneficiaryName), "The PIX beneficiary name is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.BeneficiaryCity), "The PIX beneficiary city is required.")
    .ValidateOnStart();
builder.Services.AddSingleton<ILicenseBillingConfiguration, LicenseBillingConfiguration>();
builder.Services.AddOptions<LoginRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(LoginRateLimitOptions.SectionName))
    .Validate(options => options.PermitLimit > 0 && options.WindowSeconds > 0 && options.QueueLimit >= 0 && options.LockoutMinutes > 0 && options.MaxFailedAccessAttempts > 0, "Login rate limit settings must be positive.")
    .ValidateOnStart();
builder.Services.AddOptions<ApplicationUrlOptions>()
    .Bind(builder.Configuration.GetSection(ApplicationUrlOptions.SectionName))
    .Validate(options => IsValidApplicationUrl(options.ApplicationBaseUrl), "Application base URL must be an absolute HTTP or HTTPS URL.")
    .Validate(options => IsValidApplicationUrl(options.LandingPageUrl), "Landing page URL must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services.AddSingleton<IApplicationUrlBuilder, ApplicationUrlBuilder>();
builder.Services.AddSingleton<IPublicBookingSubmissionLimiter, PublicBookingSubmissionLimiter>();
builder.Services.AddSingleton<PublicQuoteActionLimiter>();
builder.Services.AddSingleton<IComponentOperationExecutor, ComponentOperationExecutor>();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ =>
        {
            var loginOptions = builder.Configuration.GetSection(LoginRateLimitOptions.SectionName).Get<LoginRateLimitOptions>() ?? new LoginRateLimitOptions();
            return new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = loginOptions.PermitLimit,
                Window = TimeSpan.FromSeconds(loginOptions.WindowSeconds),
                QueueLimit = loginOptions.QueueLimit
            };
        }));
    options.AddPolicy("public-booking", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.AddPolicy("support-requests", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.NewGuid().ToString("N"),
        _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0
        }));
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<Microsoft.AspNetCore.Identity.IdentityOptions>(options =>
{
    var loginOptions = builder.Configuration.GetSection(LoginRateLimitOptions.SectionName).Get<LoginRateLimitOptions>() ?? new LoginRateLimitOptions();
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(loginOptions.LockoutMinutes);
    options.Lockout.MaxFailedAccessAttempts = loginOptions.MaxFailedAccessAttempts;
});
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<ICurrentCompany, CurrentCompany>();
builder.Services.AddScoped<ICurrentDisplayTimeZone, CurrentDisplayTimeZone>();
builder.Services.AddScoped<IBrazilianDisplayFormatter, BrazilianDisplayFormatter>();
builder.Services.AddScoped<IAuthorizationHandler, CompanyMemberAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CompanyOwnerAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, OperationalLicenseAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, CompanyMemberAuthorizationResultHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.CompanyMember, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new CompanyMemberRequirement());
    })
    .AddPolicy(AuthorizationPolicies.CompanyOwner, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new CompanyOwnerRequirement());
    })
    .AddPolicy(AuthorizationPolicies.CompanyOwnerLicenseRenewal, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new CompanyOwnerRequirement());
    })
    .AddPolicy(AuthorizationPolicies.OperationalCompanyLicense, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new OperationalLicenseRequirement());
    })
    .AddPolicy(AuthorizationPolicies.PlatformAdministrator, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(PlatformRoles.PlatformAdministrator);
    });

var app = builder.Build();

try
{
    await app.Services.ApplyDatabaseMigrationsAsync();
}
catch (Exception exception)
{
    app.Logger.LogCritical(exception, "Unable to apply database migrations during application startup");
    throw;
}
await PlatformAdministratorInitializer.InitializeAsync(app.Services, builder.Configuration);

app.UseRequestLocalization();
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
};

// Em produção, o Caddy é o único ponto público de entrada e
// encaminha as requisições para o ASP.NET pela rede interna do Docker.
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseStatusCodePages(statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;
    var target = response.StatusCode switch
    {
        StatusCodes.Status400BadRequest => "/error",
        StatusCodes.Status404NotFound => "/not-found",
        _ => null
    };

    if (target is not null)
    {
        response.Redirect(target);
    }

    return Task.CompletedTask;
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapStaticAssets();

app.MapAccountEndpoints();
app.MapCompanyOnboardingEndpoints();
app.MapCustomerEndpoints();
app.MapEquipmentEndpoints();
app.MapServiceEndpoints();
app.MapProfessionalEndpoints();
app.MapEmployeeInvitationEndpoints();
app.MapWorkOrderEndpoints();
app.MapMaintenanceOpportunityEndpoints();
app.MapAppointmentEndpoints();
app.MapScheduleBlockRequestEndpoints();
app.MapQuoteEndpoints();
app.MapLicenseEndpoints();
app.MapPlatformLicenseEndpoints();

app.MapSupportEndpoints();
app.MapGet("/health", () => Results.Text("Healthy"));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireRateLimiting("public-booking");

app.Run();

static bool IsValidApplicationUrl(string? value) =>
    Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
    (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
     string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
