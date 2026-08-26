using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.PublicBooking;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Quotes;

public sealed class RejectPublicQuoteUseCase(IQuoteRepository quotes, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return OperationResult<Guid>.Failure(QuoteErrors.NotFound);
        var quote = await quotes.GetForPublicTokenAsync(PublicTokenGenerator.Hash(token), cancellationToken);
        if (quote is null || quote.Status != QuoteStatus.Sent || quote.PublicTokenExpiresAtUtc <= timeProvider.GetUtcNow()) return OperationResult<Guid>.Failure(QuoteErrors.InvalidStatus);
        quote.Reject(timeProvider.GetUtcNow());
        await quotes.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(quote.Id);
    }
}
