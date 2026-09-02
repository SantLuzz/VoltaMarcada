using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.PublicBooking;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Quotes;

public sealed class PublicQuoteAccessService(IQuoteRepository quotes, TimeProvider timeProvider)
{
    public async Task<Quote?> ResolveAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var quote = await quotes.GetForPublicTokenAsync(PublicTokenGenerator.Hash(token), cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (quote is null || quote.PublicTokenExpiresAtUtc <= now)
        {
            return null;
        }

        if (quote.ExpireIfPast(DateOnly.FromDateTime(now.UtcDateTime), now))
        {
            await quotes.SaveChangesAsync(cancellationToken);
            return null;
        }

        return quote.Status is QuoteStatus.Sent or QuoteStatus.Approved or QuoteStatus.Rejected
            ? quote
            : null;
    }
}
