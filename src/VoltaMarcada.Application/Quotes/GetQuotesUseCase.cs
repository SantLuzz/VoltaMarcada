using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Customers;
using VoltaMarcada.Application.Equipment;
using VoltaMarcada.Application.WorkOrders;

namespace VoltaMarcada.Application.Quotes;

public sealed class GetQuotesUseCase(
    WorkOrderAuthorization authorization,
    IQuoteRepository repository,
    ICustomerRepository customers,
    IEquipmentRepository equipment)
{
    public async Task<IReadOnlyList<QuoteDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var quotes = await repository.GetAllForCompanyAsync(scope.CompanyId, cancellationToken);
        var customerNames = (await customers.GetAllForCompanyAsync(scope.CompanyId, GetCustomersQuery.Empty, cancellationToken))
            .ToDictionary(customer => customer.Id, customer => customer.Name);
        var equipmentDescriptions = (await equipment.GetAllForCompanyAsync(scope.CompanyId, GetEquipmentQuery.Empty, cancellationToken))
            .ToDictionary(item => item.Equipment.Id, item => QuoteDtoMapper.FormatEquipment(item.Equipment.Type, item.Equipment.Manufacturer, item.Equipment.Model));
        return quotes.Select(quote => QuoteDtoMapper.Map(
            quote,
            customerNames.GetValueOrDefault(quote.CustomerId, "Cliente indisponÃ­vel"),
            equipmentDescriptions.GetValueOrDefault(quote.EquipmentId, "Equipamento indisponÃ­vel"), equipmentDescriptions)).ToArray();
    }

    public async Task<QuoteDto?> ExecuteAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var quote = await repository.GetForCompanyAsync(quoteId, scope.CompanyId, cancellationToken);
        if (quote is null)
        {
            return null;
        }

        var customer = await customers.GetForCompanyAsync(quote.CustomerId, scope.CompanyId, cancellationToken);
        var equipmentDescriptions = (await equipment.GetAllForCompanyAsync(scope.CompanyId, GetEquipmentQuery.Empty, cancellationToken)).ToDictionary(item => item.Equipment.Id, item => QuoteDtoMapper.FormatEquipment(item.Equipment.Type, item.Equipment.Manufacturer, item.Equipment.Model));
        var selectedEquipment = await equipment.GetForCompanyAsync(quote.EquipmentId, scope.CompanyId, cancellationToken);
        return QuoteDtoMapper.Map(
            quote,
            customer?.Name ?? "Cliente indisponÃ­vel",
            selectedEquipment is null
                ? "Equipamento indisponÃ­vel"
                : QuoteDtoMapper.FormatEquipment(selectedEquipment.Type, selectedEquipment.Manufacturer, selectedEquipment.Model), equipmentDescriptions);
    }
}

internal static class QuoteDtoMapper
{
    public static QuoteDto Map(VoltaMarcada.Domain.Entities.Quote quote, string customerName, string equipmentDescription, IReadOnlyDictionary<Guid, string> equipmentDescriptions) => new(
        quote.Id, quote.CustomerId, customerName, quote.EquipmentId, equipmentDescription, quote.PrimaryServiceId, quote.Status, quote.DiscountAmount,
        quote.TotalAmount, quote.Notes, quote.ValidUntil, quote.GeneratedWorkOrderId,
        quote.Items.OrderBy(item => item.DisplayOrder).Select(item => new QuoteItemDto(item.EquipmentId, item.EquipmentId is { } equipmentId ? equipmentDescriptions.GetValueOrDefault(equipmentId, "Equipamento indisponível") : null, item.ServiceId, item.Description, item.Quantity, item.UnitPrice, item.Type)).ToArray());

    public static string FormatEquipment(string type, string? manufacturer, string? model) => string.Join(
        " ",
        new[] { type, manufacturer, model }.Where(value => !string.IsNullOrWhiteSpace(value)));
}

