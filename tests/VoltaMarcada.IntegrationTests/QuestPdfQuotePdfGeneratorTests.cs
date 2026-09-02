using VoltaMarcada.Application.Abstractions.Documents;
using VoltaMarcada.Infrastructure.Documents;
using QuestPDF.Infrastructure;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class QuestPdfQuotePdfGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_WhenDocumentIsProvided_ReturnsPdfBytes()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var generator = new QuestPdfQuotePdfGenerator();
        var document = new QuotePdfDocument(
            "Volta Marcada", "Volta Marcada Ltda", "11222333000181", "11999999999",
            "Praca da Se, 1", "01001000", "Sao Paulo", "SP", "Customer",
            new DateOnly(2026, 9, 30), null, 10, 140, null,
            [new QuotePdfLine("Maintenance", "Service", 1, 150, 150)]);

        var content = await generator.GenerateAsync(document);

        Assert.True(content.AsSpan().StartsWith("%PDF"u8));
        Assert.True(content.Length > 1_000);
    }
}
