namespace VoltaMarcada.Application.Equipment;

public sealed record GetEquipmentQuery(string? SearchTerm)
{
    public static GetEquipmentQuery Empty { get; } = new((string?)null);

    public string? SearchFilter => string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm.Trim();
}
