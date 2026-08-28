using MudBlazor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
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
using VoltaMarcada.Web.Configuration;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Application.PublicBooking;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
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
builder.Services.AddOptions<ApplicationUrlOptions>()
    .Bind(builder.Configuration.GetSection(ApplicationUrlOptions.SectionName))
    .Validate(options => IsValidApplicationUrl(options.ApplicationBaseUrl), "Application base URL must be an absolute HTTP or HTTPS URL.")
    .Validate(options => IsValidApplicationUrl(options.LandingPageUrl), "Landing page URL must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services.AddSingleton<IApplicationUrlBuilder, ApplicationUrlBuilder>();
builder.Services.AddSingleton<IPublicBookingSubmissionLimiter, PublicBookingSubmissionLimiter>();
builder.Services.AddSingleton<PublicQuoteActionLimiter>();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("public-booking", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<ICurrentCompany, CurrentCompany>();
builder.Services.AddScoped<IAuthorizationHandler, CompanyMemberAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CompanyOwnerAuthorizationHandler>();
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
    });

var app = builder.Build();

await app.Services.ApplyDatabaseMigrationsAsync();
await PlatformAdministratorInitializer.InitializeAsync(app.Services, builder.Configuration);

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
});

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireRateLimiting("public-booking");

app.Run();

static bool IsValidApplicationUrl(string? value) =>
    Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
    (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
     string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
