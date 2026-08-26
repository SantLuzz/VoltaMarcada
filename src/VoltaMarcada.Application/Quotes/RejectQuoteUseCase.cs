using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Quotes;

public sealed class RejectQuoteUseCase(IQuoteRepository quotes, WorkOrderAuthorization authorization, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var quote = await quotes.GetForCompanyAsync(quoteId, scope.CompanyId, cancellationToken);
        if (quote is null) return OperationResult<Guid>.Failure(QuoteErrors.NotFound);
        if (quote.Status != QuoteStatus.Sent) return OperationResult<Guid>.Failure(QuoteErrors.InvalidStatus);
        quote.Reject(timeProvider.GetUtcNow());
        await quotes.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(quote.Id);
    }
}
