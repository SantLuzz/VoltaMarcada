using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Equipment;
using VoltaMarcada.Domain.Entities;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class EquipmentHistoryTests
{
    [Fact]
    public async Task ExecuteAsync_WhenEquipmentBelongsToCurrentCompany_ReturnsTwentyRecordPage()
    {
        var companyId = Guid.NewGuid();
        var equipmentId = Guid.NewGuid();
        var repository = new EquipmentRepositoryStub(
            companyId,
            equipmentId,
            new EquipmentHistoryPageProjection(
                new EquipmentHistoryHeaderDto(equipmentId, "Air conditioner - LG Dual Inverter", "Maria Silva", "Bedroom"),
                Enumerable.Range(1, 20)
                    .Select(index => new EquipmentHistoryRecordDto(
                        $"OS #{index:000000}",
                        "Preventive maintenance",
                        "John Technician",
                        new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero).AddDays(-index),
                        6,
                        6,
                        3,
                        2,
                        1,
                        250m,
                        "Completed successfully.",
                        new DateOnly(2027, 3, 1)))
                    .ToArray(),
                23));
        var useCase = new GetEquipmentHistoryUseCase(new CurrentCompanyStub(companyId), repository);

        var result = await useCase.ExecuteAsync(equipmentId, offset: 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value!.Records.Count);
        Assert.Equal(23, result.Value.TotalRecordCount);
        Assert.Equal(20, result.Value.LoadedRecordCount);
        Assert.True(result.Value.HasMore);
        Assert.Equal(0, repository.LastOffset);
        Assert.Equal(20, repository.LastPageSize);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEquipmentDoesNotBelongToCurrentCompany_ReturnsNotFound()
    {
        var repository = new EquipmentRepositoryStub(Guid.NewGuid(), Guid.NewGuid(), null);
        var useCase = new GetEquipmentHistoryUseCase(new CurrentCompanyStub(Guid.NewGuid()), repository);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), offset: 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(EquipmentErrors.NotFound.Code, result.Error!.Code);
    }

    private sealed class CurrentCompanyStub(Guid companyId) : ICurrentCompany
    {
        public Task<Guid?> FindCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(companyId);

        public Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(companyId);
    }

    private sealed class EquipmentRepositoryStub(
        Guid companyId,
        Guid equipmentId,
        EquipmentHistoryPageProjection? page) : IEquipmentRepository
    {
        public int? LastOffset { get; private set; }

        public int? LastPageSize { get; private set; }

        public Task<EquipmentHistoryPageProjection?> GetHistoryPageForCompanyAsync(
            Guid requestedEquipmentId,
            Guid requestedCompanyId,
            int offset,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            LastOffset = offset;
            LastPageSize = pageSize;
            return Task.FromResult(requestedCompanyId == companyId && requestedEquipmentId == equipmentId ? page : null);
        }

        public Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<EquipmentListItem>> GetAllForCompanyAsync(Guid requestedCompanyId, GetEquipmentQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<EquipmentListItem>>([]);

        public Task<IReadOnlyList<EquipmentListItem>> GetAllForCustomerAsync(Guid customerId, Guid requestedCompanyId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<EquipmentListItem>>([]);

        public Task<Equipment?> GetForCompanyAsync(Guid requestedEquipmentId, Guid requestedCompanyId, CancellationToken cancellationToken = default) => Task.FromResult<Equipment?>(null);

        public Task<Equipment?> GetActiveForCustomerByTypeAndManufacturerAsync(Guid requestedCompanyId, Guid customerId, string type, string? manufacturer, CancellationToken cancellationToken = default) => Task.FromResult<Equipment?>(null);

        public Task<bool> ExistsForCustomerAsync(Guid customerId, Guid requestedCompanyId, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public void Remove(Equipment equipment) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
