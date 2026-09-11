using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Payments;

public sealed record CompanyPaymentSettingsDto(bool PaymentControlEnabled, string? PixKey, PixKeyType? PixKeyType);

public sealed record UpdateCompanyPaymentSettingsCommand(bool PaymentControlEnabled, string? PixKey, PixKeyType? PixKeyType);
