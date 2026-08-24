using ServiceSync.Application.Abstractions.Operations;
using FluentValidation;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Domain.Entities;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Application.Customers;

public sealed class CreateCustomerUseCase(
    ICurrentCompany currentCompany,
    ICustomerRepository repository,
    IValidator<CreateCustomerCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid || !BrazilianDocument.TryNormalize(command.Document, out var normalizedDocument))
        {
            return OperationResult<Guid?>.Failure(CustomerErrors.InvalidInput);
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        if (await repository.DocumentExistsAsync(companyId, normalizedDocument, cancellationToken: cancellationToken))
        {
            return OperationResult<Guid?>.Failure(CustomerErrors.DuplicateDocument);
        }

        if (!await repository.AreLocationsValidAsync(command.Addresses, cancellationToken))
        {
            return OperationResult<Guid?>.Failure(CustomerErrors.InvalidLocation);
        }

        var addresses = CreateAddresses(command.Addresses);
        var customer = Customer.Create(
            companyId,
            command.Name,
            normalizedDocument,
            command.WhatsApp,
            command.Email,
            command.Notes,
            addresses,
            timeProvider.GetUtcNow());

        await repository.AddAsync(customer, cancellationToken);
        var persistenceOutcome = await repository.SaveChangesAsync(cancellationToken);
        return persistenceOutcome == CustomerPersistenceOutcome.DuplicateDocument
            ? OperationResult<Guid?>.Failure(CustomerErrors.DuplicateDocument)
            : OperationResult<Guid?>.Success(customer.Id);
    }

    internal static IReadOnlyList<CustomerAddress> CreateAddresses(IReadOnlyCollection<CustomerAddressInput> inputs) =>
        inputs.Select(input => CustomerAddress.Create(
            input.AddressType,
            input.IsPrimary,
            input.Street,
            input.Number,
            input.PostalCode,
            input.Complement,
            input.StateId,
            input.CityId)).ToArray();
}
