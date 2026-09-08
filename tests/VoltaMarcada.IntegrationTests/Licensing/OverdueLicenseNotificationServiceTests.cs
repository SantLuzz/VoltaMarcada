using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using VoltaMarcada.Application.Abstractions.Communication;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Identity;
using VoltaMarcada.Infrastructure.Licensing;
using VoltaMarcada.Infrastructure.Persistence;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Licensing;

public sealed class OverdueLicenseNotificationServiceTests
{
    [Fact]
    public async Task SendOverdueNotificationsAsync_sends_once_per_owner_per_day()
    {
        await using var fixture = await Fixture.CreateAsync();

        await fixture.Service.SendOverdueNotificationsAsync(CancellationToken.None);
        await fixture.Service.SendOverdueNotificationsAsync(CancellationToken.None);

        Assert.Single(fixture.Sender.Messages);
        await using var db = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        Assert.Single(await db.LicenseOverdueNotifications.ToListAsync());
    }

    [Fact]
    public async Task SendOverdueNotificationsAsync_does_not_record_notification_when_email_delivery_fails()
    {
        await using var fixture = await Fixture.CreateAsync(throwOnSend: true);

        await fixture.Service.SendOverdueNotificationsAsync(CancellationToken.None);

        await using var db = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        Assert.Empty(await db.LicenseOverdueNotifications.ToListAsync());
    }
    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(ServiceProvider services, EmailSenderStub sender, OverdueLicenseNotificationService service) { Services = services; Sender = sender; Service = service; }
        public ServiceProvider Services { get; }
        public EmailSenderStub Sender { get; }
        public OverdueLicenseNotificationService Service { get; }
        public static async Task<Fixture> CreateAsync(bool throwOnSend = false)
        {
            var services = new ServiceCollection();
            services.AddDbContextFactory<AppDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            var sender = new EmailSenderStub(throwOnSend); services.AddSingleton<IEmailSender>(sender);
            var provider = services.BuildServiceProvider();
            await using (var db = provider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext())
            {
                var companyId = Guid.NewGuid(); var ownerId = Guid.NewGuid(); var now = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
                var license = CompanyLicense.Create(companyId, now.AddDays(-60), 14); license.Renew(now.AddDays(-45));
                db.CompanyLicenses.Add(license); db.CompanyUsers.Add(CompanyUser.Create(ownerId, companyId, CompanyUserRole.Owner, now)); db.Users.Add(new ApplicationUser { Id = ownerId, Email = "owner@example.com", UserName = "owner@example.com" }); await db.SaveChangesAsync();
            }
            return new Fixture(provider, sender, new OverdueLicenseNotificationService(provider.GetRequiredService<IServiceScopeFactory>(), new FixedTimeProvider(new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero)), NullLogger<OverdueLicenseNotificationService>.Instance));
        }
        public ValueTask DisposeAsync() => Services.DisposeAsync();
    }
    private sealed class EmailSenderStub(bool throwOnSend) : IEmailSender { public List<EmailMessage> Messages { get; } = []; public Task SendAsync(EmailMessage email, CancellationToken cancellationToken = default) { if (throwOnSend) { throw new InvalidOperationException("Simulated email delivery failure."); } Messages.Add(email); return Task.CompletedTask; } }
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
