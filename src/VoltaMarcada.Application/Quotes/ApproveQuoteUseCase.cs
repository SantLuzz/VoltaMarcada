using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.Quotes;

public sealed record QuoteApprovalResult(Guid QuoteId, Guid WorkOrderId, bool AlreadyApproved);

public sealed class ApproveQuoteUseCase(
    IQuoteRepository quotes,
    IWorkOrderRepository workOrders,
    IServiceRequestRepository serviceRequests,
    IServiceRepository services,
    WorkOrderChecklistFactory checklistFactory,
    WorkOrderAuthorization authorization,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<QuoteApprovalResult>> ExecuteAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        await using var transaction = await quotes.BeginTransactionAsync(cancellationToken);
        var quote = await quotes.GetForCompanyForUpdateAsync(quoteId, scope.CompanyId, cancellationToken);
        if (quote is null) return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.NotFound);
        if (quote.Status == QuoteStatus.Approved && quote.GeneratedWorkOrderId is { } existingId) return OperationResult<QuoteApprovalResult>.Success(new QuoteApprovalResult(quote.Id, existingId, true));
        if (quote.Status != QuoteStatus.Sent) return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.InvalidStatus);
        var now = timeProvider.GetUtcNow();
        if (quote.ExpireIfPast(DateOnly.FromDateTime(now.UtcDateTime), now))
        {
            await quotes.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.InvalidStatus);
        }
        var service = await services.GetForCompanyAsync(quote.PrimaryServiceId, scope.CompanyId, cancellationToken);
        if (service is null) return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.InvalidInput);
        var items = QuoteWorkOrderItemMapper.ToWorkOrderItemDefinitions(quote.Items);
        var checklistItems = await checklistFactory.CreateAsync(scope.CompanyId, items, cancellationToken);
        var order = WorkOrder.Create(scope.CompanyId, quote.CustomerId, quote.EquipmentId, quote.PrimaryServiceId, null, currentUser.UserId, WorkOrderPriority.Normal, null, items, checklistItems, now, quote.Id, quote.DiscountAmount);
        quote.Approve(order.Id, now);
        if (quote.ServiceRequestId is { } serviceRequestId)
        {
            var serviceRequest = await serviceRequests.GetForCompanyAsync(serviceRequestId, scope.CompanyId, cancellationToken);
            serviceRequest?.CompleteFromQuote(order.Id, now);
        }
        await workOrders.AddAsync(order, cancellationToken);
        await quotes.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<QuoteApprovalResult>.Success(new QuoteApprovalResult(quote.Id, order.Id, false));
    }
}
