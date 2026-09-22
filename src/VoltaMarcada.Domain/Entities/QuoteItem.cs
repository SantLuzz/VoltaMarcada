using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Domain.Entities;

public sealed class QuoteItem
{
    private QuoteItem() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid QuoteId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid? EquipmentId { get; private set; }
    public Guid? ServiceId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public QuoteItemType Type { get; private set; }
    public int DisplayOrder { get; private set; }
    public decimal TotalAmount => Quantity * UnitPrice;

    internal static QuoteItem Create(Guid companyId, Guid quoteId, QuoteItemDefinition definition, int displayOrder)
    {
        if (companyId == Guid.Empty || quoteId == Guid.Empty) throw new DomainException("Company and quote IDs are required.");
        if (definition is null || string.IsNullOrWhiteSpace(definition.Description) || definition.Description.Trim().Length > 500) throw new DomainException("Quote item description is invalid.");
        if (definition.Quantity <= 0 || definition.UnitPrice < 0 || !Enum.IsDefined(definition.Type) || displayOrder < 0) throw new DomainException("Quote item is invalid.");
        var equipmentId = NormalizeOptionalId(definition.EquipmentId);
        var serviceId = NormalizeOptionalId(definition.ServiceId);
        if (definition.Type == QuoteItemType.Service && serviceId is null) throw new DomainException("Service quote items must reference a service.");
        return new QuoteItem { Id = Guid.NewGuid(), CompanyId = companyId, QuoteId = quoteId, Description = definition.Description.Trim(), EquipmentId = equipmentId, ServiceId = serviceId, Quantity = definition.Quantity, UnitPrice = definition.UnitPrice, Type = definition.Type, DisplayOrder = displayOrder };
    }
    private static Guid? NormalizeOptionalId(Guid? value) => value is null || value == Guid.Empty ? null : value;

}
