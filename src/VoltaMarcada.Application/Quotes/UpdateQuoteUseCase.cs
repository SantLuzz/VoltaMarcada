using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.Quotes;

public sealed class UpdateQuoteUseCase(IQuoteRepository quotes, WorkOrderAuthorization authorization, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(Guid quoteId, UpdateQuoteCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Items.Count is 0 or > 100 || command.DiscountAmount < 0 || command.ValidUntil < DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
        {
            return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
        }

        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var quote = await quotes.GetForCompanyAsync(quoteId, scope.CompanyId, cancellationToken);
        if (quote is null) return OperationResult<Guid>.Failure(QuoteErrors.NotFound);

        try
        {
            quote.UpdateDraft(command.Items.Select(item => new QuoteItemDefinition(item.Description, item.Quantity, item.UnitPrice, item.Type)).ToArray(), command.DiscountAmount, command.Notes, command.ValidUntil, timeProvider.GetUtcNow());
            await quotes.SaveChangesAsync(cancellationToken);
            return OperationResult<Guid>.Success(quote.Id);
        }
        catch (DomainException)
        {
            return OperationResult<Guid>.Failure(QuoteErrors.InvalidStatus);
        }
    }
}
