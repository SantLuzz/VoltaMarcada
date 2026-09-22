using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.PublicBooking;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.Quotes;

public sealed class ApprovePublicQuoteUseCase(IQuoteRepository quotes, IWorkOrderRepository workOrders, IServiceRequestRepository serviceRequests, IServiceRepository services, WorkOrderChecklistFactory checklistFactory, TimeProvider timeProvider)
{
    public async Task<OperationResult<QuoteApprovalResult>> ExecuteAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.NotFound);
        await using var transaction = await quotes.BeginTransactionAsync(cancellationToken);
        var quote = await quotes.GetForPublicTokenForUpdateAsync(PublicTokenGenerator.Hash(token), cancellationToken);
        if (quote is null) return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.NotFound);
        if (quote.Status == QuoteStatus.Approved && quote.GeneratedWorkOrderId is { } id) return OperationResult<QuoteApprovalResult>.Success(new QuoteApprovalResult(quote.Id, id, true));
        var now = timeProvider.GetUtcNow();
        if (quote.Status != QuoteStatus.Sent || quote.PublicTokenExpiresAtUtc <= now)
        {
            return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.InvalidStatus);
        }
        if (quote.ExpireIfPast(DateOnly.FromDateTime(now.UtcDateTime), now))
        {
            await quotes.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.InvalidStatus);
        }
        var service = await services.GetForCompanyAsync(quote.PrimaryServiceId, quote.CompanyId, cancellationToken);
        if (service is null) return OperationResult<QuoteApprovalResult>.Failure(QuoteErrors.InvalidInput);
        var items = QuoteWorkOrderItemMapper.ToWorkOrderItemDefinitions(quote.Items);
        var checklistItems = await checklistFactory.CreateAsync(quote.CompanyId, items, cancellationToken);
        var order = WorkOrder.Create(quote.CompanyId, quote.CustomerId, quote.EquipmentId, quote.PrimaryServiceId, null, quote.CreatedByUserId, WorkOrderPriority.Normal, null, items, checklistItems, now, quote.Id, quote.DiscountAmount);
        quote.Approve(order.Id, now);
        if (quote.ServiceRequestId is { } serviceRequestId)
        {
            var serviceRequest = await serviceRequests.GetForCompanyAsync(serviceRequestId, quote.CompanyId, cancellationToken);
            serviceRequest?.CompleteFromQuote(order.Id, now);
        }
        await workOrders.AddAsync(order, cancellationToken);
        await quotes.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<QuoteApprovalResult>.Success(new QuoteApprovalResult(quote.Id, order.Id, false));
    }
}
