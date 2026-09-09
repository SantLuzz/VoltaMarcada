using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoltaMarcada.Application;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Endpoints;
using VoltaMarcada.Web.Identity;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class MaintenanceOpportunityManualEndpointsTests
{
    [Fact]
    public async Task Post_creates_manual_opportunity_for_operational_owner()
    {
        await using var fixture = await Fixture.CreateAsync();

        var response = await fixture.PostAsync(fixture.OwnerClient, fixture.ValidRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var opportunity = await fixture.GetStoredOpportunityAsync();
        var created = await response.Content.ReadFromJsonAsync<CreatedMaintenanceOpportunityResponse>();
        Assert.Equal(opportunity.Id, created?.OpportunityId);
        Assert.Null(opportunity.PreviousWorkOrderId);
    }

    [Theory]
    [InlineData(CompanyUserRole.Employee, CompanyLicenseAccessMode.Operational, HttpStatusCode.Forbidden)]
    [InlineData(CompanyUserRole.Owner, CompanyLicenseAccessMode.OwnerReadOnly, HttpStatusCode.Forbidden)]
    public async Task Post_requires_owner_and_operational_license(
        CompanyUserRole role,
        CompanyLicenseAccessMode access,
        HttpStatusCode expected)
    {
        await using var fixture = await Fixture.CreateAsync();

        var response = await fixture.PostAsync(fixture.CreateClient(role, access), fixture.ValidRequest);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Post_returns_validation_problem_for_invalid_input()
    {
        await using var fixture = await Fixture.CreateAsync();
        var invalidRequest = fixture.ValidRequest with
        {
            Recommendation = fixture.ValidRequest.Recommendation with { Notes = " " }
        };

        var response = await fixture.PostAsync(fixture.OwnerClient, invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("Revise os dados da manutenção e tente novamente.", problem?.Title);
        Assert.Empty(await fixture.GetStoredOpportunitiesAsync());
    }

    private sealed record CreatedMaintenanceOpportunityResponse(Guid OpportunityId);

    private sealed record ProblemResponse(string Title);

    private sealed class Fixture(WebApplication app, Guid companyId, Guid customerId, Guid equipmentId, Guid serviceId) : IAsyncDisposable
    {
        private static readonly DateTimeOffset Now = new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);
        private readonly WebApplication app = app;
        private readonly Guid companyId = companyId;

        public Guid CustomerId { get; } = customerId;

        public Guid EquipmentId { get; } = equipmentId;

        public Guid ServiceId { get; } = serviceId;

        public HttpClient OwnerClient { get; private set; } = null!;

        public CreateManualMaintenanceOpportunityCommand ValidRequest => new(
            CustomerId,
            EquipmentId,
            new MaintenanceRecommendationInput(90, null, ServiceId, "Annual maintenance"));

        public static async Task<Fixture> CreateAsync()
        {
            var databaseName = Guid.NewGuid().ToString();
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddRouting();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAuthentication(TestAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
            builder.Services.AddApplication();
            builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
            builder.Services.AddSingleton<ISchedulingConfiguration, SchedulingConfigurationStub>();
            builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
            builder.Services.AddScoped<ICurrentUser, CurrentUser>();
            builder.Services.AddScoped<ICurrentCompany, CurrentCompany>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<IEquipmentRepository, EquipmentRepository>();
            builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
            builder.Services.AddScoped<IMaintenanceOpportunityRepository, MaintenanceOpportunityRepository>();
            builder.Services.AddScoped<IEmployeeInvitationRepository, EmployeeInvitationRepository>();
            builder.Services.AddScoped<IProfessionalRepository, ProfessionalRepository>();
            builder.Services.AddScoped<ILicenseRepository, LicenseRepository>();
            builder.Services.AddScoped<IAuthorizationHandler, CompanyOwnerAuthorizationHandler>();
            builder.Services.AddScoped<IAuthorizationHandler, OperationalLicenseAuthorizationHandler>();
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy(AuthorizationPolicies.CompanyOwner, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new CompanyOwnerRequirement());
                })
                .AddPolicy(AuthorizationPolicies.OperationalCompanyLicense, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new OperationalLicenseRequirement());
                });

            var app = builder.Build();
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                if (Guid.TryParse(context.Request.Headers["X-Test-User"], out var userId))
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                        TestAuthenticationHandler.SchemeName));
                }

                await next(context);
            });
            app.UseAuthorization();
            app.MapMaintenanceOpportunityEndpoints();

            var companyId = Guid.NewGuid();
            var customer = Customer.Create(
                companyId, "Maria Silva", "52998224725", "11999999999", null, null,
                [CustomerAddress.Create(CustomerAddressType.Residence, true, "Rua A", "10", "01001000", null, Guid.NewGuid(), Guid.NewGuid())],
                Now);
            var equipment = Equipment.Create(companyId, customer.Id, "Air conditioner", "Carrier", "XPower", null, null, null, null, null, Now);
            var service = Service.Create(companyId, "Filter replacement", null, 60, ServicePriceType.Hidden, null, false, false, false, [], Now);
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.AddRange(customer, equipment, service);
                await dbContext.SaveChangesAsync();
            }

            await app.StartAsync();
            var fixture = new Fixture(app, companyId, customer.Id, equipment.Id, service.Id);
            fixture.OwnerClient = fixture.CreateClient(CompanyUserRole.Owner, CompanyLicenseAccessMode.Operational, companyId);
            return fixture;
        }

        public HttpClient CreateClient(CompanyUserRole role, CompanyLicenseAccessMode access) =>
            CreateClient(role, access, Guid.NewGuid());

        public async Task<HttpResponseMessage> PostAsync(HttpClient client, CreateManualMaintenanceOpportunityCommand request) =>
            await client.PostAsJsonAsync("/maintenance-opportunities/", request);

        public async Task<MaintenanceOpportunity> GetStoredOpportunityAsync() =>
            Assert.Single(await GetStoredOpportunitiesAsync());

        public async Task<IReadOnlyList<MaintenanceOpportunity>> GetStoredOpportunitiesAsync()
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.MaintenanceOpportunities.ToListAsync();
        }

        public async ValueTask DisposeAsync()
        {
            OwnerClient.Dispose();
            await app.DisposeAsync();
        }

        private HttpClient CreateClient(CompanyUserRole role, CompanyLicenseAccessMode access, Guid companyId)
        {
            var userId = Guid.NewGuid();
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.CompanyUsers.Add(CompanyUser.Create(userId, companyId, role, Now));
            dbContext.CompanyLicenses.Add(CreateLicense(companyId, access));
            dbContext.SaveChanges();

            var client = app.GetTestClient();
            client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
            return client;
        }

        private static CompanyLicense CreateLicense(Guid companyId, CompanyLicenseAccessMode access) => access switch
        {
            CompanyLicenseAccessMode.Operational => CompanyLicense.Create(companyId, Now, 14),
            CompanyLicenseAccessMode.OwnerReadOnly => CompanyLicense.Create(companyId, Now.AddDays(-18), 14),
            _ => throw new ArgumentOutOfRangeException(nameof(access), access, null)
        };
    }

    private sealed class SchedulingConfigurationStub : ISchedulingConfiguration
    {
        public int SlotIntervalMinutes => 30;
        public int AvailabilitySearchDays => 7;
        public int AvailabilityResultLimit => 10;
        public string DefaultTimeZoneId => "America/Sao_Paulo";
        public IReadOnlySet<string> AllowedTimeZoneIds { get; } = new HashSet<string>(StringComparer.Ordinal) { "America/Sao_Paulo" };
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }
}
