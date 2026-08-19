using FluentValidation;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Application.Customers;

public sealed class UpdateCustomerUseCase(
    ICurrentCompany currentCompany,
    ICustomerRepository repository,
    IValidator<UpdateCustomerCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<CustomerOperationResult> ExecuteAsync(
        Guid customerId,
        UpdateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid || !BrazilianDocument.TryNormalize(command.Document, out var normalizedDocument))
        {
            return CustomerOperationResult.InvalidInput();
        }

        if (!await repository.AreLocationsValidAsync(command.Addresses, cancellationToken))
        {
            return CustomerOperationResult.InvalidLocation();
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var customer = await repository.GetForCompanyAsync(customerId, companyId, cancellationToken);
        if (customer is null)
        {
            return CustomerOperationResult.NotFound();
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
        await repository.SaveChangesAsync(cancellationToken);

        return CustomerOperationResult.Success(customer.Id);
    }
}
