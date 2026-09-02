using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Professionals;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Web.Formatting;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class DisplayFormattingTests
{
    [Fact]
    public async Task GetTimeZoneAsync_ShouldUseTheCurrentProfessionalsTimeZone()
    {
        var companyId = Guid.NewGuid();
        var user = new CurrentUserStub();
        var professional = Professional.Create(companyId, Guid.NewGuid(), [Guid.NewGuid()], "America/Campo_Grande", DateTimeOffset.UtcNow);
        var resolver = new CurrentDisplayTimeZone(
            new CurrentCompanyStub(companyId),
            user,
            new ProfessionalRepositoryStub(new ProfessionalListItem(professional, "Professional", "professional@example.com", [], [])),
            new SchedulingConfigurationStub());

        var timeZone = await resolver.GetTimeZoneAsync();

        Assert.Equal("America/Campo_Grande", timeZone.Id);
    }

    [Fact]
    public async Task GetTimeZoneAsync_ShouldUseTheDefaultWhenTheCurrentUserHasNoProfessional()
    {
        var resolver = new CurrentDisplayTimeZone(
            new CurrentCompanyStub(Guid.NewGuid()),
            new CurrentUserStub(),
            new ProfessionalRepositoryStub(null),
            new SchedulingConfigurationStub());

        var timeZone = await resolver.GetTimeZoneAsync();

        Assert.Equal("America/Sao_Paulo", timeZone.Id);
    }

    [Fact]
    public void Formatter_ShouldUseBrazilianCurrencyAndTheProvidedTimeZone()
    {
        var formatter = new BrazilianDisplayFormatter();
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        Assert.Equal("R$ 150,00", formatter.FormatCurrency(150m));
        Assert.Equal("02/09/2026 09:00", formatter.FormatDateTime(new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero), timeZone));
    }

    private sealed class CurrentCompanyStub(Guid companyId) : ICurrentCompany
    {
        public Task<Guid?> FindCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(companyId);

        public Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(companyId);
    }

    private sealed class CurrentUserStub : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid UserId { get; } = Guid.NewGuid();
    }

    private sealed class SchedulingConfigurationStub : ISchedulingConfiguration
    {
        public int SlotIntervalMinutes => 30;

        public int AvailabilitySearchDays => 7;

        public int AvailabilityResultLimit => 10;

        public string DefaultTimeZoneId => "America/Sao_Paulo";

        public IReadOnlySet<string> AllowedTimeZoneIds { get; } = new HashSet<string>(["America/Sao_Paulo", "America/Campo_Grande"], StringComparer.Ordinal);
    }

    private sealed class ProfessionalRepositoryStub(ProfessionalListItem? currentProfessional) : IProfessionalRepository
    {
        public Task AddAsync(Professional professional, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProfessionalListItem>> GetAllForCompanyAsync(Guid companyId, string? searchFilter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Professional?> GetForCompanyAsync(Guid professionalId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProfessionalListItem?> GetForCurrentUserAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult(currentProfessional);
        public Task<IReadOnlyList<CompanyMemberOption>> GetAvailableCompanyMembersAsync(Guid companyId, Guid? professionalId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProfessionalServiceOption>> GetActiveServicesAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsCompanyMemberAvailableAsync(Guid companyUserId, Guid companyId, Guid? excludedProfessionalId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AreActiveServicesValidAsync(Guid companyId, IReadOnlyCollection<Guid> serviceIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, string>> GetTimeZoneIdsAsync(Guid companyId, IReadOnlyCollection<Guid> professionalIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(Guid companyId, IReadOnlyCollection<Guid> professionalIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
