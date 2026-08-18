using ServiceSync.Domain.Exceptions;

namespace ServiceSync.Domain.Entities;

public sealed class City
{
    private City()
    {
    }

    public Guid Id { get; private set; }

    public Guid StateId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public static City Create(Guid id, Guid stateId, string name)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("City ID is required.");
        }

        if (stateId == Guid.Empty)
        {
            throw new DomainException("State ID is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("City name is required.");
        }

        return new City
        {
            Id = id,
            StateId = stateId,
            Name = name.Trim()
        };
    }
}
