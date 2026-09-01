using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Quotes;

public sealed record PublicQuoteLineDto(string Description, decimal Quantity, decimal UnitPrice, decimal TotalAmount, QuoteItemType Type);
public sealed record PublicQuoteDto(string CompanyName, string CompanyWhatsApp, QuoteStatus Status, DateOnly ValidUntil, string? Notes, decimal DiscountAmount, decimal TotalAmount, IReadOnlyList<PublicQuoteLineDto> Items);

public sealed class GetPublicQuoteUseCase(PublicQuoteAccessService publicQuoteAccess, ICompanyRepository companies)
{
    public async Task<PublicQuoteDto?> ExecuteAsync(string? token, CancellationToken cancellationToken = default)
    {
        var quote = await publicQuoteAccess.ResolveAsync(token, cancellationToken);
        if (quote is null) return null;
        var company = await companies.GetByIdAsync(quote.CompanyId, cancellationToken);
        if (company is null) return null;
        return new PublicQuoteDto(company.TradeName, company.WhatsApp, quote.Status, quote.ValidUntil, quote.Notes, quote.DiscountAmount, quote.TotalAmount, quote.Items.OrderBy(x => x.DisplayOrder).Select(x => new PublicQuoteLineDto(x.Description, x.Quantity, x.UnitPrice, x.TotalAmount, x.Type)).ToArray());
    }
}
