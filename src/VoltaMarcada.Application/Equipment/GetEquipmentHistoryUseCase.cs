using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Equipment;

public sealed class GetEquipmentHistoryUseCase(
    ICurrentCompany currentCompany,
    IEquipmentRepository repository)
{
    private const int PageSize = 20;

    public async Task<OperationResult<EquipmentHistoryPageDto>> ExecuteAsync(
        Guid equipmentId,
        int offset,
        CancellationToken cancellationToken = default)
    {
        if (equipmentId == Guid.Empty || offset < 0)
        {
            return OperationResult<EquipmentHistoryPageDto>.Failure(EquipmentErrors.InvalidInput);
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var projection = await repository.GetHistoryPageForCompanyAsync(
            equipmentId,
            companyId,
            offset,
            PageSize,
            cancellationToken);

        return projection is null
            ? OperationResult<EquipmentHistoryPageDto>.Failure(EquipmentErrors.NotFound)
            : OperationResult<EquipmentHistoryPageDto>.Success(
                new EquipmentHistoryPageDto(
                    projection.Equipment,
                    projection.Records,
                    offset,
                    projection.TotalRecordCount));
    }
}
