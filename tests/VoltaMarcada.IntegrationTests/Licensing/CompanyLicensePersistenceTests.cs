using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Application.CompanyOnboarding;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Licensing;

public sealed class CompanyLicensePersistenceTests
{
    private readonly CompanyOnboardingFixture fixture = new();

    [Fact]
    public async Task CompleteCompanyOnboarding_creates_a_trial_license()
    {
        var companyId = await fixture.CompleteCompanyOnboardingAsync();

        await using var db = fixture.CreateDbContext();
        var license = await db.CompanyLicenses.SingleAsync(value => value.CompanyId == companyId);

        Assert.Equal(CompanyLicenseStatus.Trial, license.Status);
        Assert.Equal(fixture.Clock.GetUtcNow().AddDays(14), license.TrialEndsAtUtc);
    }

    private sealed class CompanyOnboardingFixture
    {
        private readonly DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        private readonly Guid stateId = Guid.NewGuid();
        private readonly Guid cityId = Guid.NewGuid();
        private readonly Guid userId = Guid.NewGuid();

        public FixedTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero));

        public async Task<Guid> CompleteCompanyOnboardingAsync()
        {
            await SeedLocationAsync();

            await using var db = CreateDbContext();
            var useCase = new CompleteCompanyOnboardingUseCase(
                new CurrentUser(userId),
                new CompanyOnboardingRepository(db),
                new UnusedFileStorage(),
                new CompleteCompanyOnboardingCommandValidator(),
                new FixedLicenseTrialConfiguration(),
                Clock);

            var result = await useCase.ExecuteAsync(
                new CompleteCompanyOnboardingCommand(
                    "Volta Marcada Servicos Ltda",
                    "Volta Marcada",
                    "11222333000181",
                    "11999999999",
                    "01001000",
                    "Praca da Se, 1",
                    stateId,
                    cityId,
                    null),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            return Assert.IsType<Guid>(result.Value);
        }

        public AppDbContext CreateDbContext() => new(options);

        private async Task SeedLocationAsync()
        {
            await using var db = CreateDbContext();
            if (await db.States.AnyAsync())
            {
                return;
            }

            db.States.Add(State.Create(stateId, "Sao Paulo", "SP"));
            db.Cities.Add(City.Create(cityId, stateId, "Sao Paulo"));
            await db.SaveChangesAsync();
        }
    }

    private sealed class CurrentUser(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid UserId => userId;
    }

    public sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FixedLicenseTrialConfiguration : ICompanyLicenseTrialConfiguration
    {
        public int TrialDays => 14;
    }

    private sealed class UnusedFileStorage : IFileStorage
    {
        public Task<FileStorageUploadResult> UploadAsync(FileStorageUploadRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FileStorageDownloadResult> GetAsync(string key, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
