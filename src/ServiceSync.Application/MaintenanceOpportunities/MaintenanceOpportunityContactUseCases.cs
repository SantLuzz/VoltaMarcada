using ServiceSync.Application.Abstractions.Communication;
using ServiceSync.Application.Abstractions.Operations;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Domain.Entities;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.MaintenanceOpportunities;

public sealed class PrepareMaintenanceOpportunityWhatsAppContactUseCase(
    IMaintenanceOpportunityRepository opportunityRepository,
    ICustomerContactRepository contactRepository,
    IWhatsAppLinkBuilder whatsAppLinkBuilder,
    MaintenanceOpportunityOwnerAuthorization authorization,
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

        if (context.Status is not MaintenanceOpportunityStatus.Pending and not MaintenanceOpportunityStatus.Contacted)
        {
            return OperationResult<PreparedWhatsAppContact>.Failure(CustomerContactErrors.InvalidStatus);
        }

        var message = CreateMessage(context);
        if (!whatsAppLinkBuilder.TryBuild(context.CustomerWhatsApp, message, out var link))
        {
            return OperationResult<PreparedWhatsAppContact>.Failure(CustomerContactErrors.WhatsAppUnavailable);
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

    private static string CreateMessage(MaintenanceOpportunityContactContext context) =>
        $"Ol\u00E1 {context.CustomerName}, recomendamos {context.RecommendedServiceName} para {context.EquipmentDescription}. A pr\u00F3xima manuten\u00E7\u00E3o \u00E9 recomendada para {context.DueDate:dd/MM/yyyy}. Podemos agendar?";
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
            opportunity.Status is not MaintenanceOpportunityStatus.Pending and not MaintenanceOpportunityStatus.Contacted)
        {
            return OperationResult.Failure(CustomerContactErrors.InvalidStatus);
        }

        var now = timeProvider.GetUtcNow();
        contact.MarkAsSent(now);
        opportunity.MarkAsContacted(now);
        await contactRepository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
