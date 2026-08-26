using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;
namespace VoltaMarcada.Application.Quotes;
public sealed class CreateQuoteUseCase(IQuoteRepository quotes, ICustomerRepository customers, IEquipmentRepository equipment, IServiceRepository services, WorkOrderAuthorization authorization, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(CreateQuoteCommand command, CancellationToken cancellationToken = default)
    {
        if (command.CustomerId == Guid.Empty || command.EquipmentId == Guid.Empty || command.PrimaryServiceId == Guid.Empty || command.Items.Count is 0 or > 100 || command.DiscountAmount < 0 || command.ValidUntil < DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)) return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var customer = await customers.GetForCompanyAsync(command.CustomerId, scope.CompanyId, cancellationToken);
        var service = await services.GetForCompanyAsync(command.PrimaryServiceId, scope.CompanyId, cancellationToken);
        if (customer is null || service is null || !service.IsActive) return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
        var selectedEquipment = await equipment.GetForCompanyAsync(command.EquipmentId, scope.CompanyId, cancellationToken);
        if (selectedEquipment is null || selectedEquipment.CustomerId != customer.Id) return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
        try
        {
            var quote = Quote.Create(scope.CompanyId, customer.Id, command.EquipmentId, service.Id, null, command.Items.Select(x => new QuoteItemDefinition(x.Description, x.Quantity, x.UnitPrice, x.Type)).ToArray(), command.DiscountAmount, command.Notes, command.ValidUntil, timeProvider.GetUtcNow());
            await quotes.AddAsync(quote, cancellationToken); await quotes.SaveChangesAsync(cancellationToken); return OperationResult<Guid>.Success(quote.Id);
        }
        catch (DomainException) { return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput); }
    }
}
