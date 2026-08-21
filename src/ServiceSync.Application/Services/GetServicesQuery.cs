namespace ServiceSync.Application.Services;

public sealed record GetServicesQuery(string? SearchTerm)
{
    public static GetServicesQuery Empty { get; } = new((string?)null);

    public string? SearchFilter => string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm.Trim();
}
