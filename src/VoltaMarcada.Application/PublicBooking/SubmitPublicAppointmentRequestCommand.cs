using VoltaMarcada.Application.Customers;

namespace VoltaMarcada.Application.PublicBooking;

public sealed record SubmitPublicAppointmentRequestCommand(
    string CompanySlug,
    Guid ServiceId,
    Guid ProfessionalId,
    DateOnly Date,
    TimeOnly StartTime,
    string CustomerName,
    string CustomerDocument,
    string CustomerWhatsApp,
    string? CustomerEmail,
    IReadOnlyCollection<CustomerAddressInput> Addresses);
