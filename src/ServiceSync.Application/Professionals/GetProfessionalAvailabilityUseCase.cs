using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Professionals;

public sealed class GetProfessionalAvailabilityUseCase(
    ICurrentUser currentUser,
    ICurrentCompany currentCompany,
    IProfessionalRepository professionalRepository,
    IProfessionalAvailabilityRepository availabilityRepository,
    ProfessionalOwnerAuthorization ownerAuthorization)
{
    public async Task<ProfessionalAvailabilityDto?> ExecuteAsync(
        Guid professionalId,
        CancellationToken cancellationToken = default)
    {
        if (professionalId == Guid.Empty)
        {
            return null;
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var isOwner = await ownerAuthorization.IsOwnerAsync(companyId, cancellationToken);
        if (isOwner)
        {
            if (!await availabilityRepository.ProfessionalExistsAsync(professionalId, companyId, cancellationToken))
            {
                return null;
            }
        }
        else
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            {
                return null;
            }

            var currentProfessional = await professionalRepository.GetForCurrentUserAsync(
                currentUser.UserId,
                companyId,
                cancellationToken);
            if (currentProfessional?.Professional.Id != professionalId)
            {
                return null;
            }
        }

        var availability = await availabilityRepository.GetAvailabilityAsync(
            professionalId,
            companyId,
            trackChanges: false,
            cancellationToken);
        var blocks = await availabilityRepository.GetScheduleBlocksAsync(professionalId, companyId, cancellationToken);

        return new ProfessionalAvailabilityDto(
            professionalId,
            availability is null
                ? null
                : availability.DailySchedules
                    .OrderBy(schedule => ((int)schedule.Weekday + 6) % 7)
                    .Select(DailyScheduleDto.FromEntity)
                    .ToArray(),
            blocks.Select(ScheduleBlockDto.FromEntity).ToArray());
    }
}
