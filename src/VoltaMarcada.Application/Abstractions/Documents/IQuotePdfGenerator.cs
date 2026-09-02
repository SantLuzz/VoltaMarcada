namespace VoltaMarcada.Application.Abstractions.Documents;

public sealed record QuotePdfLine(
    string Description,
    string ItemTypeLabel,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalAmount);

public sealed record QuotePdfDocument(
    string CompanyTradeName,
    string CompanyLegalName,
    string CompanyDocument,
    string CompanyWhatsApp,
    string CompanyAddress,
    string CompanyPostalCode,
    string CompanyCityName,
    string CompanyStateAbbreviation,
    string CustomerName,
    DateOnly ValidUntil,
    string? Notes,
    decimal DiscountAmount,
    decimal TotalAmount,
    byte[]? LogoContent,
    IReadOnlyList<QuotePdfLine> Lines);

public sealed record QuotePdfFile(byte[] Content, string FileName);

public interface IQuotePdfGenerator
{
    Task<byte[]> GenerateAsync(QuotePdfDocument document, CancellationToken cancellationToken = default);
}
