using VoltaMarcada.Application.Abstractions.Operations;
using FluentValidation;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.Customers;

public sealed class UpdateCustomerUseCase(
    ICurrentCompany currentCompany,
    ICustomerRepository repository,
    IValidator<UpdateCustomerCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(
        Guid customerId,
        UpdateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid || !BrazilianDocument.TryNormalize(command.Document, out var normalizedDocument))
        {
            return OperationResult<Guid?>.Failure(CustomerErrors.InvalidInput);
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var customer = await repository.GetForCompanyAsync(customerId, companyId, cancellationToken);
        if (customer is null)
        {
            return OperationResult<Guid?>.Failure(CustomerErrors.NotFound);
        }

        if (await repository.DocumentExistsAsync(
                companyId,
                normalizedDocument,
                customer.Id,
                cancellationToken))
        {
            return OperationResult<Guid?>.Failure(CustomerErrors.DuplicateDocument);
        }

        if (!await repository.AreLocationsValidAsync(command.Addresses, cancellationToken))
        {
            return OperationResult<Guid?>.Failure(CustomerErrors.InvalidLocation);
        }

        var addresses = CreateCustomerUseCase.CreateAddresses(command.Addresses);
        repository.RemoveAddresses(customer);
        customer.Update(
            command.Name,
            normalizedDocument,
            command.WhatsApp,
            command.Email,
            command.Notes,
            timeProvider.GetUtcNow());
        customer.ReplaceAddresses(addresses);
        var persistenceOutcome = await repository.SaveChangesAsync(cancellationToken);
        return persistenceOutcome == CustomerPersistenceOutcome.DuplicateDocument
            ? OperationResult<Guid?>.Failure(CustomerErrors.DuplicateDocument)
            : OperationResult<Guid?>.Success(customer.Id);
    }
}
