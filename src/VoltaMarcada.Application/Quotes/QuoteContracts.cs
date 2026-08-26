using VoltaMarcada.Domain.Enums;
namespace VoltaMarcada.Application.Quotes;
public sealed record QuoteItemInput(string Description, decimal Quantity, decimal UnitPrice, QuoteItemType Type);
public sealed record CreateQuoteCommand(Guid CustomerId, Guid? EquipmentId, Guid PrimaryServiceId, IReadOnlyList<QuoteItemInput> Items, decimal DiscountAmount, string? Notes, DateOnly ValidUntil);
public sealed record QuoteSendResult(Guid QuoteId, string PublicUrl, string? WhatsAppUrl);
