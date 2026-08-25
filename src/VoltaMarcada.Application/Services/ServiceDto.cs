using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Services;

public sealed record ServiceDto(
    Guid Id,
    string Name,
    string? Description,
    int DurationMinutes,
    ServicePriceType PriceType,
    decimal? BasePrice,
    bool AllowOnlineBooking,
    bool RequireBeforePhotos,
    bool RequireAfterPhotos,
    bool IsActive,
    IReadOnlyList<ServiceChecklistItemDto> ChecklistItems)
{
    internal static ServiceDto FromEntity(global::VoltaMarcada.Domain.Entities.Service service) =>
        new(
            service.Id,
            service.Name,
            service.Description,
            service.DurationMinutes,
            service.PriceType,
            service.BasePrice,
            service.AllowOnlineBooking,
            service.RequireBeforePhotos,
            service.RequireAfterPhotos,
            service.IsActive,
            service.ChecklistItems
                .OrderBy(item => item.DisplayOrder)
                .Select(item => new ServiceChecklistItemDto(item.Description, item.IsRequired))
                .ToArray());
}

public sealed record ServiceChecklistItemDto(string Description, bool IsRequired);
