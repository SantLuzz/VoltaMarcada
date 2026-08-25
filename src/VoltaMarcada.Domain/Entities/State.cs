using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class State
{
    private State()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Abbreviation { get; private set; } = string.Empty;

    public static State Create(Guid id, string name, string abbreviation)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("State ID is required.");
        }

        var normalizedAbbreviation = RequireText(abbreviation, "State abbreviation").ToUpperInvariant();
        if (normalizedAbbreviation.Length != 2 || normalizedAbbreviation.Any(character => character is < 'A' or > 'Z'))
        {
            throw new DomainException("State abbreviation must contain two letters.");
        }

        return new State
        {
            Id = id,
            Name = RequireText(name, "State name"),
            Abbreviation = normalizedAbbreviation
        };
    }

    private static string RequireText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return value.Trim();
    }
}
