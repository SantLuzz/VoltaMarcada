using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Quotes;

public sealed class UpdateQuoteUseCase(IQuoteRepository quotes, IEquipmentRepository equipment, IServiceRepository services, WorkOrderAuthorization authorization, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(
    Guid quoteId,
    UpdateQuoteCommand command,
    CancellationToken cancellationToken = default)
{
    if (command.Items.Count is 0 or > 100 ||
        command.DiscountAmount < 0 ||
        command.ValidUntil < DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
    {
        return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
    }

    var scope = await authorization.GetAccessScopeAsync(cancellationToken);

    var quote = await quotes.GetForCompanyAsync(
        quoteId,
        scope.CompanyId,
        cancellationToken);

    if (quote is null)
    {
        return OperationResult<Guid>.Failure(QuoteErrors.NotFound);
    }

    var serviceNames = new Dictionary<Guid, string>();

    foreach (var item in command.Items)
    {
        if (item.Type == VoltaMarcada.Domain.Enums.QuoteItemType.Service &&
            (item.ServiceId is null || item.ServiceId == Guid.Empty))
        {
            return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
        }

        if (item.EquipmentId is { } equipmentId)
        {
            var itemEquipment = await equipment.GetForCompanyAsync(
                equipmentId,
                scope.CompanyId,
                cancellationToken);

            if (itemEquipment is null || itemEquipment.CustomerId != quote.CustomerId)
            {
                return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
            }
        }

        if (item.ServiceId is { } serviceId)
        {
            var itemService = await services.GetForCompanyAsync(
                serviceId,
                scope.CompanyId,
                cancellationToken);

            if (itemService is null || !itemService.IsActive)
            {
                return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
            }

            serviceNames[serviceId] = itemService.Name;
        }
    }

    var effectiveItems = command.Items.Select(item =>
        item.Type == VoltaMarcada.Domain.Enums.QuoteItemType.Service &&
        string.IsNullOrWhiteSpace(item.Description)
            ? item with
            {
                Description = serviceNames[item.ServiceId!.Value]
            }
            : item).ToArray();

    try
    {
        quote.UpdateDraft(
            effectiveItems.Select(item => new QuoteItemDefinition(
                item.Description,
                item.Quantity,
                item.UnitPrice,
                item.Type,
                item.EquipmentId,
                item.ServiceId)).ToArray(),
            command.DiscountAmount,
            command.Notes,
            command.ValidUntil,
            timeProvider.GetUtcNow());

        await quotes.SaveChangesAsync(cancellationToken);

        return OperationResult<Guid>.Success(quote.Id);
    }
    catch (DomainException)
    {
        return OperationResult<Guid>.Failure(QuoteErrors.InvalidStatus);
    }
}
}

