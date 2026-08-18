using ServiceSync.Domain.Exceptions;

namespace ServiceSync.Domain.Entities;

public sealed class Company
{
    private Company()
    {
    }

    public Company(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Company name is required.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
