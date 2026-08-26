using VoltaMarcada.Application.Abstractions.Communication;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Routing;
using VoltaMarcada.Application.PublicBooking;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Quotes;

public sealed class SendQuoteUseCase(
    IQuoteRepository quotes,
    ICustomerRepository customers,
    WorkOrderAuthorization authorization,
    IApplicationUrlBuilder applicationUrlBuilder,
    IWhatsAppLinkBuilder whatsAppLinkBuilder,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<QuoteSendResult>> ExecuteAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var quote = await quotes.GetForCompanyAsync(quoteId, scope.CompanyId, cancellationToken);
        if (quote is null) return OperationResult<QuoteSendResult>.Failure(QuoteErrors.NotFound);
        if (quote.Status != QuoteStatus.Draft) return OperationResult<QuoteSendResult>.Failure(QuoteErrors.InvalidStatus);
        var customer = await customers.GetForCompanyAsync(quote.CustomerId, scope.CompanyId, cancellationToken);
        if (customer is null) return OperationResult<QuoteSendResult>.Failure(QuoteErrors.NotFound);

        var now = timeProvider.GetUtcNow();
        var token = PublicTokenGenerator.Generate();
        var expiration = new DateTimeOffset(quote.ValidUntil.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        if (expiration <= now) return OperationResult<QuoteSendResult>.Failure(QuoteErrors.InvalidInput);
        quote.Send(PublicTokenGenerator.Hash(token), expiration, now);
        await quotes.SaveChangesAsync(cancellationToken);

        var publicUrl = applicationUrlBuilder.BuildApplicationUrl($"q/{token}");
        var message = $"Olá, {customer.Name}! Seu orçamento está disponível em:\n{publicUrl}";
        var hasWhatsAppLink = whatsAppLinkBuilder.TryBuild(customer.WhatsApp, message, out var whatsAppUrl);
        return OperationResult<QuoteSendResult>.Success(new QuoteSendResult(quote.Id, publicUrl, hasWhatsAppLink ? whatsAppUrl : null));
    }
}
