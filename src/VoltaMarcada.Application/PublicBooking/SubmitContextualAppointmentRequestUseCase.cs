using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.PublicBooking;

public sealed record SubmitContextualAppointmentRequestCommand(
    string Token,
    DateOnly Date,
    TimeOnly StartTime);

public sealed class SubmitContextualAppointmentRequestUseCase(
    IMaintenanceOpportunityRepository opportunityRepository,
    ICompanyRepository companyRepository,
    IAppointmentRequestRepository requestRepository,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(
        SubmitContextualAppointmentRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
            return OperationResult<Guid>.Failure(PublicBookingErrors.NotFound);

        var context = await opportunityRepository.GetBookingContextAsync(PublicTokenGenerator.Hash(command.Token), cancellationToken);
        var opportunity = context?.Opportunity;
        if (context is null || opportunity is null || !context.ServiceIsActive || opportunity.BookingTokenExpiresAtUtc <= timeProvider.GetUtcNow() ||
            opportunity.Status != MaintenanceOpportunityStatus.AwaitingCustomer)
            return OperationResult<Guid>.Failure(PublicBookingErrors.NotFound);
        if (await requestRepository.HasOpenForMaintenanceOpportunityAsync(opportunity.CompanyId, opportunity.Id, cancellationToken))
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);

        var company = await companyRepository.GetByIdAsync(opportunity.CompanyId, cancellationToken);
        if (company is null || !company.IsPublicRequestSlot(command.Date, command.StartTime) || !PublicRequestSlotTime.IsFuture(command.Date, command.StartTime, timeProvider))
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);

        var request = AppointmentRequest.CreatePublicPreference(
            opportunity.CompanyId,
            opportunity.CustomerId,
            opportunity.RecommendedServiceId,
            opportunity.EquipmentId,
            opportunity.Id,
            command.Date,
            command.StartTime,
            context.ServiceDurationMinutes,
            null,
            timeProvider.GetUtcNow());
        await requestRepository.AddAsync(request, cancellationToken);
        opportunity.RevokeBookingToken(timeProvider.GetUtcNow());
        await requestRepository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(request.Id);
    }
}
