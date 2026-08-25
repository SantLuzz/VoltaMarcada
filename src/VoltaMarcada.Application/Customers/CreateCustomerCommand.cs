namespace VoltaMarcada.Application.Customers;

public sealed record CreateCustomerCommand(
    string Name,
    string Document,
    string WhatsApp,
    string? Email,
    string? Notes,
    IReadOnlyCollection<CustomerAddressInput> Addresses);
