using VoltaMarcada.Application.Abstractions.Documents;

namespace VoltaMarcada.Application.Quotes;

public sealed class GetPublicQuotePdfUseCase(
    PublicQuoteAccessService publicQuoteAccess,
    QuotePdfDocumentFactory documentFactory,
    IQuotePdfGenerator generator)
{
    public async Task<QuotePdfFile?> ExecuteAsync(string? token, CancellationToken cancellationToken = default)
    {
        var quote = await publicQuoteAccess.ResolveAsync(token, cancellationToken);
        if (quote is null)
        {
            return null;
        }

        var document = await documentFactory.CreateAsync(quote, cancellationToken);
        if (document is null)
        {
            return null;
        }

        return new QuotePdfFile(
            await generator.GenerateAsync(document, cancellationToken),
            "orcamento.pdf");
    }
}
