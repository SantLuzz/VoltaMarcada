namespace ServiceSync.Application.Customers;

public sealed record UpdateCustomerCommand(
    string Name,
    string Document,
    string WhatsApp,
    string? Email,
    string? Notes,
    IReadOnlyCollection<CustomerAddressInput> Addresses);
