using VoltaMarcada.Application.Abstractions.Documents;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Quotes;

public sealed class GetQuotePdfUseCase(
    IQuoteRepository quotes,
    ICurrentCompany currentCompany,
    QuotePdfDocumentFactory documentFactory,
    IQuotePdfGenerator generator)
{
    public async Task<QuotePdfFile?> ExecuteAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var quote = await quotes.GetForCompanyAsync(quoteId, companyId, cancellationToken);
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
