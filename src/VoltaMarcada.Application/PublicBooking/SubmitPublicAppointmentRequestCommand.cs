using VoltaMarcada.Application.Customers;

namespace VoltaMarcada.Application.PublicBooking;

public sealed record SubmitPublicAppointmentRequestCommand(
    string CompanySlug,
    Guid ServiceId,
    DateOnly Date,
    TimeOnly StartTime,
    string CustomerName,
    string CustomerDocument,
    string CustomerWhatsApp,
    string? CustomerEmail,
    IReadOnlyCollection<CustomerAddressInput> Addresses);
