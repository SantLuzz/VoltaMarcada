using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
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
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Licensing;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Payments;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Endpoints;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Licensing;

public sealed class LicenseEndpointsTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 15, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetProof_returns_not_found_for_another_company_owner()
    {
        await using var fixture = await TestApplication.StartAsync(
            CompanyUserRole.Owner,
            CompanyLicenseAccessMode.OwnerReadOnly,
            paymentBelongsToCurrentCompany: false);

        var response = await fixture.Client.GetAsync($"/license/payments/{fixture.PaymentId}/proof");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, fixture.Storage.DownloadCount);
    }

    [Fact]
    public async Task GetProof_returns_private_bytes_to_payment_company_owner()
    {
        await using var fixture = await TestApplication.StartAsync(
            CompanyUserRole.Owner,
            CompanyLicenseAccessMode.Terminated,
            paymentBelongsToCurrentCompany: true);

        var response = await fixture.Client.GetAsync($"/license/payments/{fixture.PaymentId}/proof");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(1, fixture.Storage.DownloadCount);
    }

    [Fact]
    public async Task GetProof_forbids_company_employee()
    {
        await using var fixture = await TestApplication.StartAsync(
            CompanyUserRole.Employee,
            CompanyLicenseAccessMode.Operational,
            paymentBelongsToCurrentCompany: true);

        var response = await fixture.Client.GetAsync($"/license/payments/{fixture.PaymentId}/proof");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, fixture.Storage.DownloadCount);
    }

    [Theory]
    [InlineData(CompanyLicenseAccessMode.OwnerReadOnly)]
    [InlineData(CompanyLicenseAccessMode.Terminated)]
    public async Task SubmitProof_allows_owner_to_regularize_in_every_restricted_access_mode(
        CompanyLicenseAccessMode accessMode)
    {
        await using var fixture = await TestApplication.StartAsync(
            CompanyUserRole.Owner,
            accessMode,
            seedPayment: false);
        using var content = new MultipartFormDataContent();
        var proof = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        proof.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(proof, "proof", "proof.png");

        var request = new HttpRequestMessage(HttpMethod.Post, "/license/payments")
        {
            Content = content
        };
        var tokenResponse = await fixture.Client.GetAsync("/antiforgery-token");
        var requestToken = await tokenResponse.Content.ReadAsStringAsync();
        var cookies = tokenResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0]);
        request.Headers.Add("X-CSRF-TOKEN", requestToken);
        request.Headers.Add("Cookie", string.Join("; ", cookies));

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(fixture.Storage.LastUpload);
        Assert.Equal(FileStorageAccess.Private, fixture.Storage.LastUpload.Access);
    }

    private sealed class TestApplication(
        WebApplication app,
        HttpClient client,
        FileStorageStub storage,
        Guid paymentId) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public FileStorageStub Storage { get; } = storage;

        public Guid PaymentId { get; } = paymentId;

        public static async Task<TestApplication> StartAsync(
            CompanyUserRole role,
            CompanyLicenseAccessMode accessMode,
            bool paymentBelongsToCurrentCompany = true,
            bool seedPayment = true)
        {
            var userId = Guid.NewGuid();
            var currentCompanyId = Guid.NewGuid();
            var paymentCompanyId = paymentBelongsToCurrentCompany ? currentCompanyId : Guid.NewGuid();
            var storage = new FileStorageStub();
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddRouting();
            builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
            builder.Services.AddAuthentication(TestAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
            builder.Services.AddApplication();
            builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            builder.Services.AddScoped<VoltaMarcada.Application.Abstractions.Persistence.ILicenseRepository, LicenseRepository>();
            builder.Services.AddSingleton<ICurrentCompany>(new CurrentCompanyStub(currentCompanyId));
            builder.Services.AddSingleton<IFileStorage>(storage);
            builder.Services.AddSingleton<ILicenseBillingConfiguration, BillingConfigurationStub>();
            builder.Services.AddSingleton<VoltaMarcada.Application.Abstractions.Payments.IPixPayloadGenerator, BrCodePixPayloadGenerator>();
            builder.Services.AddScoped<IAuthorizationHandler, CompanyOwnerAuthorizationHandler>();
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy(AuthorizationPolicies.CompanyOwnerLicenseRenewal, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new CompanyOwnerRequirement());
                });

            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                "Test"));
            var app = builder.Build();
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                context.User = principal;
                await next(context);
            });
            app.UseAuthorization();
            app.UseAntiforgery();
            app.MapGet("/antiforgery-token", (HttpContext context, IAntiforgery antiforgery) =>
                Results.Text(antiforgery.GetAndStoreTokens(context).RequestToken!));
            app.MapLicenseEndpoints();

            var paymentId = Guid.Empty;
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.CompanyUsers.Add(CompanyUser.Create(userId, currentCompanyId, role, Now));
                dbContext.CompanyLicenses.Add(CreateLicense(currentCompanyId, accessMode));
                if (!paymentBelongsToCurrentCompany)
                {
                    dbContext.CompanyLicenses.Add(CompanyLicense.Create(paymentCompanyId, Now, 14));
                }

                if (seedPayment)
                {
                    var payment = LicensePayment.Create(paymentCompanyId, 49.90m, Now);
                    payment.AttachProof($"pagamentos/{paymentCompanyId}/{payment.Id}.png", "image/png", 4, Now);
                    dbContext.LicensePayments.Add(payment);
                    paymentId = payment.Id;
                    storage.Add(payment.ProofFileKey!, [0x89, 0x50, 0x4E, 0x47], "image/png");
                }

                await dbContext.SaveChangesAsync();
            }

            await app.StartAsync();
            return new TestApplication(app, app.GetTestClient(), storage, paymentId);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }

        private static CompanyLicense CreateLicense(Guid companyId, CompanyLicenseAccessMode mode) => mode switch
        {
            CompanyLicenseAccessMode.Operational => CompanyLicense.Create(companyId, Now, 14),
            CompanyLicenseAccessMode.OwnerReadOnly => CompanyLicense.Create(companyId, Now.AddDays(-18), 14),
            CompanyLicenseAccessMode.Terminated => CompanyLicense.Create(companyId, Now.AddDays(-24), 14),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private sealed class FileStorageStub : IFileStorage
    {
        private readonly Dictionary<string, FileStorageDownloadResult> files = new(StringComparer.Ordinal);

        public int DownloadCount { get; private set; }

        public FileStorageUploadRequest? LastUpload { get; private set; }

        public void Add(string key, byte[] content, string contentType) =>
            files.Add(key, new FileStorageDownloadResult(content, contentType));

        public Task<FileStorageUploadResult> UploadAsync(
            FileStorageUploadRequest request,
            CancellationToken cancellationToken)
        {
            LastUpload = request;
            var key = $"{request.Folder}/{request.FileName}";
            files[key] = new FileStorageDownloadResult([0x89, 0x50, 0x4E, 0x47], request.File.ContentType);
            return Task.FromResult(new FileStorageUploadResult(key));
        }

        public Task<FileStorageDownloadResult> GetAsync(string key, CancellationToken cancellationToken)
        {
            DownloadCount++;
            return Task.FromResult(files[key]);
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            files.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class CurrentCompanyStub(Guid companyId) : ICurrentCompany
    {
        public Task<Guid?> FindCompanyIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(companyId);

        public Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(companyId);
    }

    private sealed class BillingConfigurationStub : ILicenseBillingConfiguration
    {
        public decimal MonthlyPrice => 49.90m;

        public string PixKey => "key@example.com";

        public string BeneficiaryName => "VoltaMarcada";

        public string BeneficiaryCity => "SAO PAULO";

        public long MaximumProofSizeBytes => 5_000_000;
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
