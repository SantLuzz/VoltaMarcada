using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class CustomerContactRepository(AppDbContext dbContext) : ICustomerContactRepository
{
    public Task AddAsync(CustomerContact contact, CancellationToken cancellationToken = default) =>
        dbContext.CustomerContacts.AddAsync(contact, cancellationToken).AsTask();

    public Task<CustomerContact?> GetForCompanyAsync(
        Guid contactId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.CustomerContacts.SingleOrDefaultAsync(
            contact => contact.Id == contactId && contact.CompanyId == companyId,
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
