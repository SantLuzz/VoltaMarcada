namespace VoltaMarcada.Application.Customers;

public sealed record GetCustomersQuery(string? SearchTerm)
{
    public static GetCustomersQuery Empty { get; } = new((string?)null);

    public string? SearchFilter => string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm.Trim();

    public string DocumentSearchFilter => new string((SearchTerm ?? string.Empty)
        .Where(char.IsDigit)
        .ToArray());
}
