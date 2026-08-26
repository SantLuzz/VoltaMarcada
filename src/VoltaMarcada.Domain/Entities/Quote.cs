using VoltaMarcada.Domain.Abstractions.Tenancy;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Domain.Entities;

public sealed class Quote : ICompanyScopedEntity
{
    private readonly List<QuoteItem> _items = [];

    private Quote() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? EquipmentId { get; private set; }
    public Guid PrimaryServiceId { get; private set; }
    public Guid? ServiceRequestId { get; private set; }
    public Guid? GeneratedWorkOrderId { get; private set; }
    public QuoteStatus Status { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public string? Notes { get; private set; }
    public DateOnly ValidUntil { get; private set; }
    public string? PublicTokenHash { get; private set; }
    public DateTimeOffset? PublicTokenExpiresAtUtc { get; private set; }
    public DateTimeOffset? PublicTokenRevokedAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<QuoteItem> Items => _items;
    public decimal TotalAmount => _items.Sum(item => item.TotalAmount) - DiscountAmount;

    public static Quote Create(Guid companyId, Guid customerId, Guid? equipmentId, Guid primaryServiceId, Guid? serviceRequestId, IReadOnlyCollection<QuoteItemDefinition> items, decimal discountAmount, string? notes, DateOnly validUntil, DateTimeOffset now)
    {
        if (companyId == Guid.Empty || customerId == Guid.Empty || primaryServiceId == Guid.Empty) throw new DomainException("Company, customer, and primary service IDs are required.");
        if (validUntil == DateOnly.MinValue || discountAmount < 0) throw new DomainException("Quote validity or discount is invalid.");
        var utcNow = now.ToUniversalTime();
        var quote = new Quote { Id = Guid.NewGuid(), CompanyId = companyId, CustomerId = customerId, EquipmentId = NormalizeId(equipmentId), PrimaryServiceId = primaryServiceId, ServiceRequestId = NormalizeId(serviceRequestId), Status = QuoteStatus.Draft, DiscountAmount = discountAmount, Notes = NormalizeNotes(notes), ValidUntil = validUntil, CreatedAtUtc = utcNow, UpdatedAtUtc = utcNow };
        quote.ReplaceItems(items);
        return quote;
    }

    public void UpdateDraft(IReadOnlyCollection<QuoteItemDefinition> items, decimal discountAmount, string? notes, DateOnly validUntil, DateTimeOffset now)
    {
        EnsureStatus(QuoteStatus.Draft);
        if (discountAmount < 0 || validUntil == DateOnly.MinValue) throw new DomainException("Quote validity or discount is invalid.");
        ReplaceItems(items); DiscountAmount = discountAmount; Notes = NormalizeNotes(notes); ValidUntil = validUntil; UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Send(string tokenHash, DateTimeOffset tokenExpiresAtUtc, DateTimeOffset now)
    {
        EnsureStatus(QuoteStatus.Draft);
        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Trim().Length > 128 || tokenExpiresAtUtc <= now) throw new DomainException("Quote public token is invalid.");
        PublicTokenHash = tokenHash.Trim(); PublicTokenExpiresAtUtc = tokenExpiresAtUtc.ToUniversalTime(); PublicTokenRevokedAtUtc = null; Status = QuoteStatus.Sent; SentAtUtc = UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Approve(Guid workOrderId, DateTimeOffset now)
    {
        EnsureStatus(QuoteStatus.Sent);
        if (workOrderId == Guid.Empty) throw new DomainException("Work order ID is required.");
        GeneratedWorkOrderId = workOrderId; Status = QuoteStatus.Approved; DecidedAtUtc = UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Reject(DateTimeOffset now)
    {
        EnsureStatus(QuoteStatus.Sent);
        Status = QuoteStatus.Rejected; DecidedAtUtc = UpdatedAtUtc = now.ToUniversalTime();
    }

    public bool ExpireIfPast(DateOnly currentDate, DateTimeOffset now)
    {
        if (Status != QuoteStatus.Sent || currentDate <= ValidUntil) return false;
        Status = QuoteStatus.Expired; UpdatedAtUtc = now.ToUniversalTime(); return true;
    }

    public void RevokePublicToken(DateTimeOffset now)
    {
        if (PublicTokenHash is null) return;
        PublicTokenRevokedAtUtc = now.ToUniversalTime(); UpdatedAtUtc = PublicTokenRevokedAtUtc.Value;
    }

    private void ReplaceItems(IReadOnlyCollection<QuoteItemDefinition> items)
    {
        if (items is null || items.Count is 0 or > 100 || items.Count(item => item.Type == QuoteItemType.Service) == 0) throw new DomainException("Quote must contain at least one service item.");
        _items.Clear();
        for (var index = 0; index < items.Count; index++) _items.Add(QuoteItem.Create(CompanyId, Id, items.ElementAt(index), index));
    }

    private void EnsureStatus(QuoteStatus status)
    {
        if (Status != status) throw new DomainException("Quote is not in the required status.");
    }

    private static Guid? NormalizeId(Guid? value) => value is null || value == Guid.Empty ? null : value;
    private static string? NormalizeNotes(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > 4000) throw new DomainException("Quote notes cannot exceed 4000 characters.");
        return normalized;
    }
}
