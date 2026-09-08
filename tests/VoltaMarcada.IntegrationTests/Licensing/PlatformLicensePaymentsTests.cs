using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Licensing;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Licensing;

public sealed class PlatformLicensePaymentsTests
{
    [Fact]
    public async Task List_returns_payment_review_data_without_a_tenant_context()
    {
        var repository = new RepositoryStub();
        var result = await new GetPlatformLicensePaymentsUseCase(repository).ExecuteAsync();

        var payment = Assert.Single(result);
        Assert.Equal("Acme", payment.CompanyName);
        Assert.Equal("owner@acme.test", payment.OwnerEmail);
    }

    private sealed class RepositoryStub : ILicenseRepository
    {
        public Task<IReadOnlyList<PlatformLicensePaymentDto>> GetPlatformPaymentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlatformLicensePaymentDto>>([new(Guid.NewGuid(), "Acme", "owner@acme.test", null, 49.90m, DateTimeOffset.UtcNow, null, null, null)]);
        public Task<VoltaMarcada.Application.Abstractions.Persistence.CompanyLicenseMembership?> GetMembershipAndLicenseAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<VoltaMarcada.Application.Abstractions.Persistence.CompanyLicenseMembership?>(null);
        public Task<VoltaMarcada.Domain.Entities.CompanyLicense?> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<VoltaMarcada.Domain.Entities.CompanyLicense?>(null);
        public Task<bool> HasPendingPaymentAsync(Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<VoltaMarcada.Domain.Entities.LicensePayment?> GetPendingPaymentAsync(Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<VoltaMarcada.Domain.Entities.LicensePayment?>(null);
        public Task<VoltaMarcada.Domain.Entities.LicensePayment?> GetPaymentAsync(Guid paymentId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<VoltaMarcada.Domain.Entities.LicensePayment?>(null);
        public Task<LicensePaymentPersistenceOutcome> AddPaymentAsync(VoltaMarcada.Domain.Entities.LicensePayment payment, CancellationToken cancellationToken = default) => Task.FromResult(LicensePaymentPersistenceOutcome.Success);
    }
}
