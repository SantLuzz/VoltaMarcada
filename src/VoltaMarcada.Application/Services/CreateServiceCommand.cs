using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Services;

public sealed record CreateServiceCommand(
    string Name,
    string? Description,
    int DurationMinutes,
    ServicePriceType PriceType,
    decimal? BasePrice,
    bool AllowOnlineBooking,
    bool RequireBeforePhotos,
    bool RequireAfterPhotos,
    IReadOnlyList<ServiceChecklistItemInput> ChecklistItems) : IServiceCommand;
