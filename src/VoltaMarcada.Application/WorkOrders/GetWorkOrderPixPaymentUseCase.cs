using VoltaMarcada.Application.Abstractions.Communication;
using VoltaMarcada.Application.Abstractions.Payments;
using VoltaMarcada.Application.Abstractions.Persistence;

namespace VoltaMarcada.Application.WorkOrders;

public sealed record WorkOrderPixPaymentDto(string CopyAndPasteCode, string? WhatsAppShareLink);

public sealed class GetWorkOrderPixPaymentUseCase(
    IWorkOrderRepository workOrderRepository,
    ICustomerRepository customerRepository,
    ICompanyRepository companyRepository,
    WorkOrderAuthorization authorization,
    IPixPayloadGenerator pixPayloadGenerator,
    IWhatsAppLinkBuilder whatsAppLinkBuilder)
{
    public async Task<WorkOrderPixPaymentDto?> ExecuteAsync(
        Guid workOrderId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return null;
        }

        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var workOrder = await workOrderRepository.GetForAccessAsync(
            workOrderId,
            scope.CompanyId,
            scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty,
            cancellationToken);
        if (workOrder is null || amount > workOrder.TotalAmount)
        {
            return null;
        }

        var company = await companyRepository.GetByIdAsync(scope.CompanyId, cancellationToken);
        if (company is null || !company.PaymentControlEnabled || string.IsNullOrWhiteSpace(company.PixKey))
        {
            return null;
        }

        var profile = await companyRepository.GetQuotePdfProfileAsync(scope.CompanyId, cancellationToken);
        var customer = await customerRepository.GetForCompanyAsync(workOrder.CustomerId, scope.CompanyId, cancellationToken);
        if (profile is null || customer is null)
        {
            return null;
        }

        var pix = pixPayloadGenerator.Generate(new PixPaymentRequest(
            company.PixKey,
            profile.TradeName,
            profile.CityName,
            amount));
        var message = $"Olá, {customer.Name}! Segue o PIX para pagamento do serviço no valor de {amount:C2}: {pix.CopyAndPasteCode}";
        var hasWhatsAppLink = whatsAppLinkBuilder.TryBuild(customer.WhatsApp, message, out var whatsAppLink);

        return new WorkOrderPixPaymentDto(pix.CopyAndPasteCode, hasWhatsAppLink ? whatsAppLink : null);
    }
}
