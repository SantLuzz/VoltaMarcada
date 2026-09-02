using VoltaMarcada.Domain.Enums;
namespace VoltaMarcada.Application.Quotes;
public sealed record QuoteItemInput(string Description, decimal Quantity, decimal UnitPrice, QuoteItemType Type);
public sealed record CreateQuoteCommand(Guid CustomerId, Guid EquipmentId, Guid PrimaryServiceId, IReadOnlyList<QuoteItemInput> Items, decimal DiscountAmount, string? Notes, DateOnly ValidUntil, Guid? SourceAppointmentId = null, Guid? MaintenanceOpportunityId = null);
public sealed record QuoteSendResult(Guid QuoteId, string PublicUrl, string? WhatsAppUrl);
public sealed record UpdateQuoteCommand(IReadOnlyList<QuoteItemInput> Items, decimal DiscountAmount, string? Notes, DateOnly ValidUntil);
public sealed record QuoteDto(Guid Id, Guid CustomerId, string CustomerName, Guid EquipmentId, string EquipmentDescription, Guid PrimaryServiceId, QuoteStatus Status, decimal DiscountAmount, decimal TotalAmount, string? Notes, DateOnly ValidUntil, Guid? GeneratedWorkOrderId, IReadOnlyList<QuoteItemInput> Items);
