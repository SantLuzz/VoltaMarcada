using VoltaMarcada.Application.Abstractions.Communication;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Routing;
using VoltaMarcada.Application.PublicBooking;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.MaintenanceOpportunities;

public sealed class PrepareMaintenanceOpportunityWhatsAppContactUseCase(
    IMaintenanceOpportunityRepository opportunityRepository,
    ICustomerContactRepository contactRepository,
    IWhatsAppLinkBuilder whatsAppLinkBuilder,
    MaintenanceOpportunityOwnerAuthorization authorization,
    IApplicationUrlBuilder applicationUrlBuilder,
    IPublicBookingConfiguration publicBookingConfiguration,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<PreparedWhatsAppContact>> ExecuteAsync(
        Guid opportunityId,
        CancellationToken cancellationToken = default)
    {
        if (opportunityId == Guid.Empty)
        {
            return OperationResult<PreparedWhatsAppContact>.Failure(CustomerContactErrors.InvalidInput);
        }

        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null)
        {
            return OperationResult<PreparedWhatsAppContact>.Failure(CustomerContactErrors.Forbidden);
        }

        var context = await opportunityRepository.GetContactContextAsync(opportunityId, companyId.Value, cancellationToken);
        if (context is null)
        {
            return OperationResult<PreparedWhatsAppContact>.Failure(CustomerContactErrors.NotFound);
        }

        if (context.Status is not MaintenanceOpportunityStatus.Pending and not MaintenanceOpportunityStatus.Contacted and not MaintenanceOpportunityStatus.AwaitingCustomer)
        {
            return OperationResult<PreparedWhatsAppContact>.Failure(CustomerContactErrors.InvalidStatus);
        }

        var token = PublicTokenGenerator.Generate();
        var bookingLink = applicationUrlBuilder.BuildApplicationUrl($"agendar/manutencao/{token}");
        var message = CreateMessage(context, bookingLink);
        if (!whatsAppLinkBuilder.TryBuild(context.CustomerWhatsApp, message, out var link))
        {
            return OperationResult<PreparedWhatsAppContact>.Failure(CustomerContactErrors.WhatsAppUnavailable);
        }

        var opportunity = await opportunityRepository.GetForCompanyAsync(opportunityId, companyId.Value, cancellationToken);
        if (opportunity is null)
        {
            return OperationResult<PreparedWhatsAppContact>.Failure(CustomerContactErrors.NotFound);
        }

        try
        {
            opportunity.SetBookingToken(
                PublicTokenGenerator.Hash(token),
                timeProvider.GetUtcNow().AddDays(publicBookingConfiguration.MaintenanceLinkLifetimeDays),
                timeProvider.GetUtcNow());
        }
        catch (DomainException)
        {
            return OperationResult<PreparedWhatsAppContact>.Failure(CustomerContactErrors.InvalidStatus);
        }

        var contact = CustomerContact.Prepare(
            companyId.Value,
            context.CustomerId,
            context.OpportunityId,
            CustomerContactChannel.WhatsAppLink,
            message,
            timeProvider.GetUtcNow());
        await contactRepository.AddAsync(contact, cancellationToken);
        await contactRepository.SaveChangesAsync(cancellationToken);

        return OperationResult<PreparedWhatsAppContact>.Success(new PreparedWhatsAppContact(contact.Id, link));
    }

    private static string CreateMessage(MaintenanceOpportunityContactContext context, string bookingLink) =>
        $"Ol\u00E1 {context.CustomerName}, recomendamos {context.RecommendedServiceName} para {context.EquipmentDescription}. A pr\u00F3xima manuten\u00E7\u00E3o \u00E9 recomendada para {context.DueDate:dd/MM/yyyy}. Escolha um hor\u00E1rio de prefer\u00EAncia: {bookingLink}";
}

public sealed class ConfirmMaintenanceOpportunityContactSentUseCase(
    ICustomerContactRepository contactRepository,
    IMaintenanceOpportunityRepository opportunityRepository,
    MaintenanceOpportunityOwnerAuthorization authorization,
    TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(
        Guid opportunityId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        if (opportunityId == Guid.Empty || contactId == Guid.Empty)
        {
            return OperationResult.Failure(CustomerContactErrors.InvalidInput);
        }

        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null)
        {
            return OperationResult.Failure(CustomerContactErrors.Forbidden);
        }

        var contact = await contactRepository.GetForCompanyAsync(contactId, companyId.Value, cancellationToken);
        var opportunity = await opportunityRepository.GetForCompanyAsync(opportunityId, companyId.Value, cancellationToken);
        if (contact is null ||
            opportunity is null ||
            contact.OpportunityId != opportunity.Id ||
            contact.CustomerId != opportunity.CustomerId)
        {
            return OperationResult.Failure(CustomerContactErrors.NotFound);
        }

        if (contact.Status != CustomerContactStatus.Prepared ||
            opportunity.Status is not MaintenanceOpportunityStatus.Pending and not MaintenanceOpportunityStatus.Contacted and not MaintenanceOpportunityStatus.AwaitingCustomer)
        {
            return OperationResult.Failure(CustomerContactErrors.InvalidStatus);
        }

        var now = timeProvider.GetUtcNow();
        contact.MarkAsSent(now);
        if (opportunity.Status is MaintenanceOpportunityStatus.Pending or MaintenanceOpportunityStatus.Contacted)
        {
            opportunity.MarkAsContacted(now);
        }
        await contactRepository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
