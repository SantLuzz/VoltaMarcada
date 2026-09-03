using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Scheduling;

namespace VoltaMarcada.Web.Formatting;

public sealed class CurrentDisplayTimeZone(
    ICurrentCompany currentCompany,
    ICurrentUser currentUser,
    IProfessionalRepository professionalRepository,
    ISchedulingConfiguration schedulingConfiguration) : ICurrentDisplayTimeZone
{
    public async Task<TimeZoneInfo> GetTimeZoneAsync(CancellationToken cancellationToken = default)
    {
        var timeZoneId = schedulingConfiguration.DefaultTimeZoneId;
        if (currentUser.IsAuthenticated && currentUser.UserId != Guid.Empty)
        {
            var companyId = await currentCompany.FindCompanyIdAsync(cancellationToken);
            if (companyId is { } resolvedCompanyId)
            {
                var professional = await professionalRepository.GetForCurrentUserAsync(currentUser.UserId, resolvedCompanyId, cancellationToken);
                if (professional is not null)
                {
                    timeZoneId = professional.Professional.TimeZoneId;
                }
            }
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
