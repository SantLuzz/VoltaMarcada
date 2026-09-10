namespace VoltaMarcada.Application.Abstractions.Payments;

public sealed record PixPaymentRequest(
    string Key,
    string BeneficiaryName,
    string BeneficiaryCity,
    decimal Amount);

public sealed record PixPaymentPayload(string CopyAndPasteCode);

public interface IPixPayloadGenerator
{
    PixPaymentPayload Generate(PixPaymentRequest request);
}
