using ServiceSync.Application.Customers;
using ServiceSync.Domain.Enums;
using ServiceSync.Web.Authorization;

namespace ServiceSync.Web.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var customers = endpoints.MapGroup("/customers")
            .RequireAuthorization(AuthorizationPolicies.CompanyMember);

        customers.MapPost("/", CreateAsync);
        customers.MapGet("/", GetAllAsync);
        customers.MapGet("/{customerId:guid}/edit", GetForEditAsync);
        customers.MapPut("/{customerId:guid}", UpdateAsync);
        customers.MapDelete("/{customerId:guid}", DeleteAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CustomerRequest request,
        CreateCustomerUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (HasInvalidAddressCollection(request))
        {
            return InvalidInput();
        }

        var result = await useCase.ExecuteAsync(ToCreateCommand(request), cancellationToken);

        return result.Code switch
        {
            CustomerOperationResultCode.Success => Results.Created(
                $"/customers/{result.CustomerId}/edit",
                new { customerId = result.CustomerId }),
            CustomerOperationResultCode.InvalidInput => InvalidInput(),
            CustomerOperationResultCode.DuplicateDocument => Results.Conflict(
                new { error = "J\u00e1 existe um cliente com este CPF ou CNPJ." }),
            CustomerOperationResultCode.InvalidLocation => Results.BadRequest(
                new { error = "Selecione um estado e uma cidade válidos." }),
            _ => UnexpectedResult()
        };
    }

    private static async Task<IResult> GetAllAsync(
        string? search,
        GetCustomersUseCase useCase,
        CancellationToken cancellationToken)
    {
        var customers = await useCase.ExecuteAsync(
            new GetCustomersQuery(search),
            cancellationToken);
        return Results.Ok(customers);
    }

    private static async Task<IResult> GetForEditAsync(
        Guid customerId,
        GetCustomerForEditUseCase useCase,
        CancellationToken cancellationToken)
    {
        var customer = await useCase.ExecuteAsync(customerId, cancellationToken);
        return customer is null ? Results.NotFound() : Results.Ok(customer);
    }

    private static async Task<IResult> UpdateAsync(
        Guid customerId,
        CustomerRequest request,
        UpdateCustomerUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (HasInvalidAddressCollection(request))
        {
            return InvalidInput();
        }

        var result = await useCase.ExecuteAsync(customerId, ToUpdateCommand(request), cancellationToken);

        return result.Code switch
        {
            CustomerOperationResultCode.Success => Results.Ok(),
            CustomerOperationResultCode.InvalidInput => InvalidInput(),
            CustomerOperationResultCode.DuplicateDocument => Results.Conflict(
                new { error = "J\u00e1 existe um cliente com este CPF ou CNPJ." }),
            CustomerOperationResultCode.InvalidLocation => Results.BadRequest(
                new { error = "Selecione um estado e uma cidade válidos." }),
            CustomerOperationResultCode.NotFound => Results.NotFound(),
            _ => UnexpectedResult()
        };
    }

    private static async Task<IResult> DeleteAsync(
        Guid customerId,
        DeleteCustomerUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(customerId, cancellationToken);

        return result.Code switch
        {
            CustomerOperationResultCode.Success => Results.NoContent(),
            CustomerOperationResultCode.NotFound => Results.NotFound(),
            CustomerOperationResultCode.HasLinkedEquipment => Results.Conflict(
                new { error = "O cliente possui equipamentos vinculados e n\u00e3o pode ser exclu\u00eddo." }),
            _ => UnexpectedResult()
        };
    }

    private static CreateCustomerCommand ToCreateCommand(CustomerRequest request) =>
        new(
            request.Name,
            request.Document,
            request.WhatsApp,
            request.Email,
            request.Notes,
            ToAddressInputs(request.Addresses));

    private static UpdateCustomerCommand ToUpdateCommand(CustomerRequest request) =>
        new(
            request.Name,
            request.Document,
            request.WhatsApp,
            request.Email,
            request.Notes,
            ToAddressInputs(request.Addresses));

    private static IReadOnlyCollection<CustomerAddressInput> ToAddressInputs(
        IReadOnlyList<CustomerAddressRequest> addresses) =>
        addresses.Select(address => new CustomerAddressInput(
            address.AddressType,
            address.IsPrimary,
            address.Street,
            address.Number,
            address.PostalCode,
            address.Complement,
            address.StateId,
            address.CityId)).ToArray();

    private static bool HasInvalidAddressCollection(CustomerRequest request) =>
        request.Addresses is null || request.Addresses.Any(address => address is null);

    private static IResult InvalidInput() =>
        Results.BadRequest(new { error = "Revise os dados do cliente e tente novamente." });

    private static IResult UnexpectedResult() =>
        Results.Problem(statusCode: StatusCodes.Status500InternalServerError);

    public sealed record CustomerRequest(
        string Name,
        string Document,
        string WhatsApp,
        string? Email,
        string? Notes,
        IReadOnlyList<CustomerAddressRequest> Addresses);

    public sealed record CustomerAddressRequest(
        CustomerAddressType AddressType,
        bool IsPrimary,
        string Street,
        string Number,
        string PostalCode,
        string? Complement,
        Guid StateId,
        Guid CityId);
}
