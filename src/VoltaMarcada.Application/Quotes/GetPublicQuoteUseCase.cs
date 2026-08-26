using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.PublicBooking;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Quotes;

public sealed record PublicQuoteLineDto(string Description, decimal Quantity, decimal UnitPrice, decimal TotalAmount, QuoteItemType Type);
public sealed record PublicQuoteDto(QuoteStatus Status, DateOnly ValidUntil, string? Notes, decimal DiscountAmount, decimal TotalAmount, IReadOnlyList<PublicQuoteLineDto> Items);

public sealed class GetPublicQuoteUseCase(IQuoteRepository quotes, TimeProvider timeProvider)
{
    public async Task<PublicQuoteDto?> ExecuteAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var quote = await quotes.GetForPublicTokenAsync(PublicTokenGenerator.Hash(token), cancellationToken);
        if (quote is null || quote.PublicTokenExpiresAtUtc <= timeProvider.GetUtcNow()) return null;
        quote.ExpireIfPast(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime), timeProvider.GetUtcNow());
        if (quote.Status is not (QuoteStatus.Sent or QuoteStatus.Approved or QuoteStatus.Rejected)) return null;
        return new PublicQuoteDto(quote.Status, quote.ValidUntil, quote.Notes, quote.DiscountAmount, quote.TotalAmount, quote.Items.OrderBy(x => x.DisplayOrder).Select(x => new PublicQuoteLineDto(x.Description, x.Quantity, x.UnitPrice, x.TotalAmount, x.Type)).ToArray());
    }
}
