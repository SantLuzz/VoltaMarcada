using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VoltaMarcada.Application.Abstractions.Documents;

namespace VoltaMarcada.Infrastructure.Documents;

public sealed class QuestPdfQuotePdfGenerator : IQuotePdfGenerator
{
    private static readonly CultureInfo BrazilianCulture = CultureInfo.GetCultureInfo("pt-BR");

    public Task<byte[]> GenerateAsync(QuotePdfDocument document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(style => style.FontSize(10));
            page.Header().Element(container => ComposeHeader(container, document));
            page.Content().PaddingVertical(16).Column(column =>
            {
                column.Spacing(12);
                column.Item().Text("ORÇAMENTO").SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);
                column.Item().Text($"Cliente: {document.CustomerName}");
                column.Item().Text($"Válido até: {document.ValidUntil:dd/MM/yyyy}");
                column.Item().Element(container => ComposeItems(container, document));
                if (document.DiscountAmount > 0)
                    column.Item().AlignRight().Text($"Desconto: -{FormatCurrency(document.DiscountAmount)}");
                column.Item().AlignRight().Text($"Total: {FormatCurrency(document.TotalAmount)}").SemiBold().FontSize(13);
                if (!string.IsNullOrWhiteSpace(document.Notes))
                    column.Item().PaddingTop(8).Text(document.Notes);
            });
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
        })).GeneratePdf());
    }

    private static void ComposeHeader(IContainer container, QuotePdfDocument document) => container.Row(row =>
    {
        row.RelativeItem().Column(column =>
        {
            column.Item().Text(document.CompanyTradeName).SemiBold().FontSize(16);
            column.Item().Text(document.CompanyLegalName);
            column.Item().Text($"CNPJ/CPF: {document.CompanyDocument}");
            column.Item().Text($"{document.CompanyAddress} - {document.CompanyCityName}/{document.CompanyStateAbbreviation}");
            column.Item().Text($"CEP: {document.CompanyPostalCode} | WhatsApp: {document.CompanyWhatsApp}");
        });
        if (document.LogoContent is not null)
            row.ConstantItem(100).Height(60).Image(document.LogoContent).FitArea();
    });

    private static void ComposeItems(IContainer container, QuotePdfDocument document) => container.Table(table =>
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(4);
            columns.RelativeColumn(2);
            columns.RelativeColumn();
            columns.RelativeColumn(2);
            columns.RelativeColumn(2);
        });
        table.Header(header =>
        {
            header.Cell().Element(HeaderCell).Text("Descrição");
            header.Cell().Element(HeaderCell).Text("Tipo");
            header.Cell().Element(HeaderCell).AlignRight().Text("Qtd.");
            header.Cell().Element(HeaderCell).AlignRight().Text("Valor unitário");
            header.Cell().Element(HeaderCell).AlignRight().Text("Total");
        });
        foreach (var line in document.Lines)
        {
            table.Cell().Element(BodyCell).Text(line.Description);
            table.Cell().Element(BodyCell).Text(line.ItemTypeLabel);
            table.Cell().Element(BodyCell).AlignRight().Text(line.Quantity.ToString("N2", BrazilianCulture));
            table.Cell().Element(BodyCell).AlignRight().Text(FormatCurrency(line.UnitPrice));
            table.Cell().Element(BodyCell).AlignRight().Text(FormatCurrency(line.TotalAmount));
        }
    });

    private static IContainer HeaderCell(IContainer container) => container.Background(Colors.Grey.Lighten2).Padding(5).DefaultTextStyle(style => style.SemiBold());
    private static IContainer BodyCell(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
    private static string FormatCurrency(decimal value) => value.ToString("C", BrazilianCulture);
}
