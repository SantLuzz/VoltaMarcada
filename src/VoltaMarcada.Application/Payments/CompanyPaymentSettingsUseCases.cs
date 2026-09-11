using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Application.Payments;

public sealed class GetCompanyPaymentSettingsUseCase(MaintenanceOpportunityOwnerAuthorization authorization, ICompanyRepository companyRepository)
{
    public async Task<CompanyPaymentSettingsDto?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null) return null;
        var company = await companyRepository.GetByIdAsync(companyId.Value, cancellationToken);
        return company is null ? null : new CompanyPaymentSettingsDto(company.PaymentControlEnabled, company.PixKey, company.PixKeyType);
    }
}

public sealed class UpdateCompanyPaymentSettingsUseCase(MaintenanceOpportunityOwnerAuthorization authorization, ICompanyRepository companyRepository, TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(UpdateCompanyPaymentSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null) return OperationResult.Failure(CompanySettings.CompanySettingsErrors.Forbidden);
        var company = await companyRepository.GetByIdAsync(companyId.Value, cancellationToken);
        if (company is null) return OperationResult.Failure(CompanySettings.CompanySettingsErrors.NotFound);
        try { company.UpdatePaymentSettings(command.PaymentControlEnabled, command.PixKey, command.PixKeyType, timeProvider.GetUtcNow()); }
        catch (DomainException) { return OperationResult.Failure(CompanySettings.CompanySettingsErrors.InvalidInput); }
        await companyRepository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
