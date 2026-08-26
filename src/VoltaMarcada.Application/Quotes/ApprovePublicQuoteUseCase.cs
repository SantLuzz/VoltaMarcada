using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.PublicBooking;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.Quotes;

public sealed class ApprovePublicQuoteUseCase(IQuoteRepository quotes, IWorkOrderRepository workOrders, IServiceRepository services, TimeProvider timeProvider)
{
    public async Task<OperationResult<QuoteApprovalResult>> ExecuteAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.NotFound);
        var quote = await quotes.GetForPublicTokenAsync(PublicTokenGenerator.Hash(token), cancellationToken);
        if (quote is null) return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.NotFound);
        if (quote.Status == QuoteStatus.Approved && quote.GeneratedWorkOrderId is { } id) return OperationResult<QuoteApprovalResult>.Success(new QuoteApprovalResult(quote.Id, id));
        var now = timeProvider.GetUtcNow();
        if (quote.Status != QuoteStatus.Sent || quote.PublicTokenExpiresAtUtc <= now || quote.ExpireIfPast(DateOnly.FromDateTime(now.UtcDateTime), now)) return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.InvalidStatus);
        var service = await services.GetForCompanyAsync(quote.PrimaryServiceId, quote.CompanyId, cancellationToken);
        if (service is null) return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.InvalidInput);
        var serviceSeen = false;
        var items = quote.Items.Select(item =>
        {
            var type = item.Type switch { QuoteItemType.Part => WorkOrderItemType.Part, QuoteItemType.Material => WorkOrderItemType.Material, QuoteItemType.Travel => WorkOrderItemType.Travel, QuoteItemType.Service when !serviceSeen => WorkOrderItemType.Service, _ => WorkOrderItemType.Additional };
            if (item.Type == QuoteItemType.Service && !serviceSeen) serviceSeen = true;
            return new WorkOrderItemDefinition(item.Description, item.Quantity, item.UnitPrice, type);
        }).ToArray();
        var order = WorkOrder.Create(quote.CompanyId, quote.CustomerId, quote.EquipmentId, quote.PrimaryServiceId, null, quote.CreatedByUserId, WorkOrderPriority.Normal, null, items, service.ChecklistItems.OrderBy(x => x.DisplayOrder).Select(x => new WorkOrderChecklistItemDefinition(x.Description, x.IsRequired, x.DisplayOrder)).ToArray(), now, quote.Id, quote.DiscountAmount);
        quote.Approve(order.Id, now);
        await workOrders.AddAsync(order, cancellationToken);
        await quotes.SaveChangesAsync(cancellationToken);
        return OperationResult<QuoteApprovalResult>.Success(new QuoteApprovalResult(quote.Id, order.Id));
    }
}
