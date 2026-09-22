using VoltaMarcada.Application.Abstractions.Documents;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Quotes;

public sealed class QuotePdfDocumentFactory(
    ICompanyRepository companies,
    ICustomerRepository customers,
    IEquipmentRepository equipment,
    IFileStorage fileStorage)
{
    public async Task<QuotePdfDocument?> CreateAsync(Quote quote, CancellationToken cancellationToken = default)
    {
        var profile = await companies.GetQuotePdfProfileAsync(quote.CompanyId, cancellationToken);
        var customer = await customers.GetForCompanyAsync(quote.CustomerId, quote.CompanyId, cancellationToken);
        if (profile is null || customer is null)
        {
            return null;
        }

        var logoContent = await GetLogoContentAsync(profile.LogoKey, cancellationToken);
        var equipmentDescriptions = new List<string>();
        foreach (var equipmentId in quote.Items.Select(item => item.EquipmentId).Where(item => item is not null).Select(item => item!.Value).Distinct())
        {
            var equipmentItem = await equipment.GetForCompanyAsync(equipmentId, quote.CompanyId, cancellationToken);
            if (equipmentItem is not null)
            {
                equipmentDescriptions.Add(FormatEquipment(equipmentItem.Type, equipmentItem.Manufacturer, equipmentItem.Model));
            }
        }

        if (equipmentDescriptions.Count == 0)
        {
            var primaryEquipment = await equipment.GetForCompanyAsync(quote.EquipmentId, quote.CompanyId, cancellationToken);
            if (primaryEquipment is not null)
            {
                equipmentDescriptions.Add(FormatEquipment(primaryEquipment.Type, primaryEquipment.Manufacturer, primaryEquipment.Model));
            }
        }
        var lines = quote.Items
            .OrderBy(item => item.DisplayOrder)
            .Select(item => new QuotePdfLine(
                item.Description,
                GetItemTypeLabel(item.Type),
                item.Quantity,
                item.UnitPrice,
                item.TotalAmount))
            .ToArray();

        return new QuotePdfDocument(
            profile.TradeName,
            profile.LegalName,
            profile.Document,
            profile.WhatsApp,
            profile.Address,
            profile.PostalCode,
            profile.CityName,
            profile.StateAbbreviation,
            customer.Name,
            quote.ValidUntil,
            quote.Notes,
            quote.DiscountAmount,
            quote.TotalAmount,
            logoContent,
            equipmentDescriptions,
            lines);
    }

    private async Task<byte[]?> GetLogoContentAsync(string? logoKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logoKey))
        {
            return null;
        }

        try
        {
            return (await fileStorage.GetAsync(logoKey, cancellationToken)).Content;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static string GetItemTypeLabel(QuoteItemType type) => type switch
    {
        QuoteItemType.Service => "Service",
        QuoteItemType.Part => "Part",
        QuoteItemType.Material => "Material",
        QuoteItemType.Travel => "Travel",
        QuoteItemType.Custom => "Other",
        _ => "Other"
    };

    private static string FormatEquipment(string type, string? manufacturer, string? model) => string.Join(
        " ",
        new[] { type, manufacturer, model }.Where(value => !string.IsNullOrWhiteSpace(value)));
}
