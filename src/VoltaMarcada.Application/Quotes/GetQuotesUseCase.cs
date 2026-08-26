using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;

namespace VoltaMarcada.Application.Quotes;

public sealed class GetQuotesUseCase(WorkOrderAuthorization authorization, IQuoteRepository repository)
{
    public async Task<IReadOnlyList<QuoteDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var quotes = await repository.GetAllForCompanyAsync(scope.CompanyId, cancellationToken);
        return quotes.Select(QuoteDtoMapper.Map).ToArray();
    }

    public async Task<QuoteDto?> ExecuteAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var quote = await repository.GetForCompanyAsync(quoteId, scope.CompanyId, cancellationToken);
        return quote is null ? null : QuoteDtoMapper.Map(quote);
    }
}

internal static class QuoteDtoMapper
{
    public static QuoteDto Map(VoltaMarcada.Domain.Entities.Quote quote) => new(
        quote.Id, quote.CustomerId, quote.EquipmentId, quote.PrimaryServiceId, quote.Status, quote.DiscountAmount,
        quote.TotalAmount, quote.Notes, quote.ValidUntil, quote.GeneratedWorkOrderId,
        quote.Items.OrderBy(item => item.DisplayOrder).Select(item => new QuoteItemInput(item.Description, item.Quantity, item.UnitPrice, item.Type)).ToArray());
}
