using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.Quotes;

public sealed record QuoteApprovalResult(Guid QuoteId, Guid WorkOrderId);

public sealed class ApproveQuoteUseCase(
    IQuoteRepository quotes,
    IWorkOrderRepository workOrders,
    IServiceRepository services,
    WorkOrderAuthorization authorization,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<QuoteApprovalResult>> ExecuteAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var quote = await quotes.GetForCompanyAsync(quoteId, scope.CompanyId, cancellationToken);
        if (quote is null) return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.NotFound);
        if (quote.Status == QuoteStatus.Approved && quote.GeneratedWorkOrderId is { } existingId) return OperationResult<QuoteApprovalResult>.Success(new QuoteApprovalResult(quote.Id, existingId));
        if (quote.Status != QuoteStatus.Sent) return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.InvalidStatus);
        var now = timeProvider.GetUtcNow();
        if (quote.ExpireIfPast(DateOnly.FromDateTime(now.UtcDateTime), now)) { await quotes.SaveChangesAsync(cancellationToken); return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.InvalidStatus); }
        var service = await services.GetForCompanyAsync(quote.PrimaryServiceId, scope.CompanyId, cancellationToken);
        if (service is null) return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.InvalidInput);
        var hasPrimaryService = false;
        var items = quote.Items.Select(item =>
        {
            var type = item.Type switch
            {
                QuoteItemType.Part => WorkOrderItemType.Part,
                QuoteItemType.Material => WorkOrderItemType.Material,
                QuoteItemType.Travel => WorkOrderItemType.Travel,
                QuoteItemType.Service when !hasPrimaryService => WorkOrderItemType.Service,
                _ => WorkOrderItemType.Additional
            };
            if (item.Type == QuoteItemType.Service && !hasPrimaryService) hasPrimaryService = true;
            return new WorkOrderItemDefinition(item.Description, item.Quantity, item.UnitPrice, type);
        }).ToArray();
        var order = WorkOrder.Create(scope.CompanyId, quote.CustomerId, quote.EquipmentId, quote.PrimaryServiceId, null, currentUser.UserId, WorkOrderPriority.Normal, null, items, service.ChecklistItems.OrderBy(x => x.DisplayOrder).Select(x => new WorkOrderChecklistItemDefinition(x.Description, x.IsRequired, x.DisplayOrder)).ToArray(), now, quote.Id, quote.DiscountAmount);
        quote.Approve(order.Id, now);
        await workOrders.AddAsync(order, cancellationToken);
        await quotes.SaveChangesAsync(cancellationToken);
        return OperationResult<QuoteApprovalResult>.Success(new QuoteApprovalResult(quote.Id, order.Id));
    }
}
