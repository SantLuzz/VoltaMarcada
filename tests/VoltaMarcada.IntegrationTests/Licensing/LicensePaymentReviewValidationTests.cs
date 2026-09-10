using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Licensing;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Licensing;

public sealed class LicensePaymentReviewValidationTests
{
    [Fact]
    public async Task Reject_without_a_reason_returns_a_validation_error()
    {
        var result = await CreateUseCase().ExecuteAsync(Guid.NewGuid(), new(LicensePaymentReviewDecision.Reject, null));

        Assert.False(result.IsSuccess);
        Assert.Equal(LicenseErrors.RejectionReasonRequired, result.Error);
    }

    private static ReviewLicensePaymentUseCase CreateUseCase() => new(new RepositoryStub(), new CurrentUserStub(), TimeProvider.System);

    private sealed class CurrentUserStub : ICurrentUser { public bool IsAuthenticated => true; public Guid UserId => Guid.NewGuid(); }
    private sealed class RepositoryStub : ILicenseRepository
    {
        public Task<CompanyLicenseMembership?> GetMembershipAndLicenseAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<CompanyLicenseMembership?>(null);
        public Task<CompanyLicense?> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<CompanyLicense?>(null);
        public Task<bool> HasPendingPaymentAsync(Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<LicensePayment?> GetPendingPaymentAsync(Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<LicensePayment?>(null);
        public Task<LicensePayment?> GetPaymentAsync(Guid paymentId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<LicensePayment?>(null);
        public Task<LicensePaymentPersistenceOutcome> AddPaymentAsync(LicensePayment payment, CancellationToken cancellationToken = default) => Task.FromResult(LicensePaymentPersistenceOutcome.Success);
    }
}
