using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VoltaMarcada.Application.Abstractions.Communication;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Persistence;

namespace VoltaMarcada.Infrastructure.Licensing;

public sealed class OverdueLicenseNotificationService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<OverdueLicenseNotificationService> logger) : BackgroundService
{
    private static readonly TimeZoneInfo SaoPauloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SendOverdueNotificationsAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }

    public async Task SendOverdueNotificationsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var now = timeProvider.GetUtcNow();
        var sentOn = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, SaoPauloTimeZone).DateTime);
        var recipients = await (from license in db.CompanyLicenses
                                join membership in db.CompanyUsers on license.CompanyId equals membership.CompanyId
                                join user in db.Users on membership.UserId equals user.Id
                                where membership.Role == CompanyUserRole.Owner && license.Status == CompanyLicenseStatus.Paid &&
                                      license.PaidUntilUtc != null && license.PaidUntilUtc < now && license.PaidUntilUtc >= now.AddDays(-20)
                                select new { license, user }).ToListAsync(cancellationToken);
        foreach (var recipient in recipients)
        {
            if (string.IsNullOrWhiteSpace(recipient.user.Email) || await db.LicenseOverdueNotifications.AnyAsync(x => x.CompanyLicenseId == recipient.license.Id && x.OwnerUserId == recipient.user.Id && x.SentOn == sentOn, cancellationToken)) continue;
            try
            {
                await sender.SendAsync(new EmailMessage(recipient.user.Email, "Licença VoltaMarcada vencida", "<p>Sua licença VoltaMarcada está vencida. Regularize-a para manter o acesso operacional.</p>"), cancellationToken);
                db.LicenseOverdueNotifications.Add(VoltaMarcada.Domain.Entities.LicenseOverdueNotification.Create(recipient.license.Id, recipient.user.Id, sentOn, now));
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unable to send overdue license notification to {RecipientEmail}", recipient.user.Email);
            }
        }
    }
}
