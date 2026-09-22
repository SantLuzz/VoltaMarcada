using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;
using VoltaMarcada.Domain.Enums;
namespace VoltaMarcada.Application.Quotes;
public sealed class CreateQuoteUseCase(IQuoteRepository quotes, ICustomerRepository customers, IEquipmentRepository equipment, IServiceRepository services, IAppointmentRepository appointments, IMaintenanceOpportunityRepository maintenanceOpportunities, WorkOrderAuthorization authorization, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(
    CreateQuoteCommand command,
    CancellationToken cancellationToken = default)
{
    if (command.Items.Count is 0 or > 100 ||
        command.DiscountAmount < 0 ||
        command.ValidUntil < DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
    {
        return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
    }

    var scope = await authorization.GetAccessScopeAsync(cancellationToken);
    var effectiveCommand = command;
    MaintenanceOpportunity? maintenanceOpportunity = null;

    if (command.MaintenanceOpportunityId is { } opportunityId)
    {
        if (!scope.IsOwner)
        {
            return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
        }

        maintenanceOpportunity = await maintenanceOpportunities.GetForCompanyAsync(
            opportunityId,
            scope.CompanyId,
            cancellationToken);

        if (maintenanceOpportunity is null ||
            maintenanceOpportunity.Status is MaintenanceOpportunityStatus.Converted or
                MaintenanceOpportunityStatus.ClosedWithoutConversion)
        {
            return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
        }

        effectiveCommand = command with
        {
            CustomerId = maintenanceOpportunity.CustomerId,
            EquipmentId = maintenanceOpportunity.EquipmentId,
            PrimaryServiceId = maintenanceOpportunity.RecommendedServiceId
        };
    }

    if (!effectiveCommand.Items.Any(item =>
            item.EquipmentId is not null || item.ServiceId is not null))
    {
        effectiveCommand = effectiveCommand with
        {
            Items = effectiveCommand.Items.Select(item =>
                item.Type == QuoteItemType.Service
                    ? item with
                    {
                        EquipmentId = effectiveCommand.EquipmentId,
                        ServiceId = effectiveCommand.PrimaryServiceId
                    }
                    : item).ToArray()
        };
    }

    var firstLinkedItem = effectiveCommand.Items.FirstOrDefault(item =>
        item.EquipmentId is { } equipmentId &&
        equipmentId != Guid.Empty &&
        item.ServiceId is { } serviceId &&
        serviceId != Guid.Empty);

    if (firstLinkedItem is not null)
    {
        effectiveCommand = effectiveCommand with
        {
            EquipmentId = firstLinkedItem.EquipmentId!.Value,
            PrimaryServiceId = firstLinkedItem.ServiceId!.Value
        };
    }

    if (effectiveCommand.CustomerId == Guid.Empty ||
        effectiveCommand.EquipmentId == Guid.Empty ||
        effectiveCommand.PrimaryServiceId == Guid.Empty)
    {
        return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
    }

    var customer = await customers.GetForCompanyAsync(
        effectiveCommand.CustomerId,
        scope.CompanyId,
        cancellationToken);

    var primaryService = await services.GetForCompanyAsync(
        effectiveCommand.PrimaryServiceId,
        scope.CompanyId,
        cancellationToken);

    if (customer is null || primaryService is null || !primaryService.IsActive)
    {
        return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
    }

    var selectedEquipment = await equipment.GetForCompanyAsync(
        effectiveCommand.EquipmentId,
        scope.CompanyId,
        cancellationToken);

    if (selectedEquipment is null || selectedEquipment.CustomerId != customer.Id)
    {
        return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
    }

    var serviceNames = new Dictionary<Guid, string>
    {
        [primaryService.Id] = primaryService.Name
    };

    foreach (var item in effectiveCommand.Items)
    {
        if (item.Type == QuoteItemType.Service &&
            (item.ServiceId is null ||
             item.ServiceId == Guid.Empty ||
             item.EquipmentId is null ||
             item.EquipmentId == Guid.Empty))
        {
            return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
        }

        if (item.EquipmentId is { } equipmentId)
        {
            var itemEquipment = await equipment.GetForCompanyAsync(
                equipmentId,
                scope.CompanyId,
                cancellationToken);

            if (itemEquipment is null || itemEquipment.CustomerId != customer.Id)
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

    effectiveCommand = effectiveCommand with
    {
        Items = effectiveCommand.Items.Select(item =>
            item.Type == QuoteItemType.Service &&
            string.IsNullOrWhiteSpace(item.Description)
                ? item with
                {
                    Description = serviceNames[item.ServiceId!.Value]
                }
                : item).ToArray()
    };

    if (effectiveCommand.SourceAppointmentId is { } sourceAppointmentId)
    {
        var sourceAppointment = await appointments.GetForAccessAsync(
            sourceAppointmentId,
            scope.CompanyId,
            scope.IsOwner ? null : scope.ProfessionalId,
            cancellationToken);

        if (sourceAppointment is null ||
            sourceAppointment.Status != AppointmentStatus.Completed ||
            sourceAppointment.Type is not (AppointmentType.Visit or AppointmentType.Other) ||
            sourceAppointment.CustomerId != customer.Id ||
            sourceAppointment.EquipmentId is { } sourceEquipmentId &&
            sourceEquipmentId != selectedEquipment.Id)
        {
            return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
        }
    }

    try
    {
        var quote = Quote.Create(
            scope.CompanyId,
            customer.Id,
            currentUser.UserId,
            effectiveCommand.EquipmentId,
            primaryService.Id,
            null,
            effectiveCommand.SourceAppointmentId,
            effectiveCommand.Items.Select(item => new QuoteItemDefinition(
                item.Description,
                item.Quantity,
                item.UnitPrice,
                item.Type,
                item.EquipmentId,
                item.ServiceId)).ToArray(),
            effectiveCommand.DiscountAmount,
            effectiveCommand.Notes,
            effectiveCommand.ValidUntil,
            timeProvider.GetUtcNow());

        await using var transaction = await quotes.BeginTransactionAsync(cancellationToken);

        await quotes.AddAsync(quote, cancellationToken);

        maintenanceOpportunity?.Convert(
            MaintenanceOpportunityConversionType.Quote,
            quote.Id,
            timeProvider.GetUtcNow());

        await quotes.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return OperationResult<Guid>.Success(quote.Id);
    }
    catch (DomainException)
    {
        return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
    }
}
}
