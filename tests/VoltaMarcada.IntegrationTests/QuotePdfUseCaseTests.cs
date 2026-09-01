using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Application.Customers;
using VoltaMarcada.Application.PublicBooking;
using VoltaMarcada.Application.Quotes;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class QuotePdfUseCaseTests
{
    [Fact]
    public async Task ResolveAsync_WhenQuoteIsDraft_ReturnsNullAfterHashingTheToken()
    {
        var now = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        var quote = CreateQuote(now);
        var repository = new QuoteRepositoryStub(quote);
        var access = new PublicQuoteAccessService(repository, new FixedTimeProvider(now));

        var result = await access.ResolveAsync("public-token");

        Assert.Null(result);
        Assert.Equal(PublicTokenGenerator.Hash("public-token"), repository.LastPublicTokenHash);
    }

    [Fact]
    public async Task CreateAsync_WhenLogoIsUnavailable_MapsCommercialDataWithoutLogo()
    {
        var now = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        var quote = CreateQuote(now);
        var customer = CreateCustomer(quote.CompanyId, now);
        var profile = new CompanyQuotePdfProfile(
            quote.CompanyId,
            "Volta Marcada Ltda",
            "Volta Marcada",
            "11222333000181",
            "11999999999",
            "01001000",
            "Praca da Se, 1",
            "Sao Paulo",
            "SP",
            "company-logo");
        var factory = new QuotePdfDocumentFactory(
            new CompanyRepositoryProfileStub(profile),
            new CustomerRepositoryStub(customer),
            new UnavailableFileStorage());

        var document = await factory.CreateAsync(quote);

        Assert.NotNull(document);
        Assert.Null(document!.LogoContent);
        Assert.Equal("Volta Marcada", document.CompanyTradeName);
        Assert.Equal("Customer", document.CustomerName);
        Assert.Equal("Service", document.Lines.Single().ItemTypeLabel);
        Assert.Equal(150, document.TotalAmount);
    }

    [Fact]
    public async Task GetQuotePdfProfileAsync_WhenCompanyExists_ReturnsAddressAndLocationText()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var now = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        var state = State.Create(Guid.NewGuid(), "Sao Paulo", "SP");
        var city = City.Create(Guid.NewGuid(), state.Id, "Sao Paulo");
        var company = Company.Create(
            "Volta Marcada Ltda",
            "Volta Marcada",
            "11222333000181",
            "11999999999",
            "01001000",
            "Praca da Se, 1",
            state.Id,
            city.Id,
            null,
            "volta-marcada",
            now);

        await using var context = new AppDbContext(options);
        await context.States.AddAsync(state);
        await context.Cities.AddAsync(city);
        await context.Companies.AddAsync(company);
        await context.SaveChangesAsync();

        var profile = await new CompanyRepository(context).GetQuotePdfProfileAsync(company.Id);

        Assert.NotNull(profile);
        Assert.Equal("Volta Marcada", profile.TradeName);
        Assert.Equal("Sao Paulo", profile.CityName);
        Assert.Equal("SP", profile.StateAbbreviation);
    }

    private static Quote CreateQuote(DateTimeOffset now) => Quote.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        null,
        [new QuoteItemDefinition("Air conditioner maintenance", 1, 150, QuoteItemType.Service)],
        0,
        null,
        new DateOnly(2026, 9, 30),
        now);

    private static Customer CreateCustomer(Guid companyId, DateTimeOffset now) => Customer.Create(
        companyId,
        "Customer",
        "12345678909",
        "11999999999",
        null,
        null,
        [CustomerAddress.Create(CustomerAddressType.Home, true, "Test Street", "1", "01001000", null, Guid.NewGuid(), Guid.NewGuid())],
        now);

    private sealed class QuoteRepositoryStub(Quote publicQuote) : IQuoteRepository
    {
        public string? LastPublicTokenHash { get; private set; }

        public Task<IQuoteTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Quote quote, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Quote?> GetForCompanyAsync(Guid quoteId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<Quote?>(null);
        public Task<Quote?> GetForCompanyForUpdateAsync(Guid quoteId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<Quote?>(null);
        public Task<IReadOnlyList<Quote>> GetAllForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Quote>>([]);
        public Task<Quote?> GetForPublicTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            LastPublicTokenHash = tokenHash;
            return Task.FromResult<Quote?>(publicQuote);
        }

        public Task<Quote?> GetForPublicTokenForUpdateAsync(string tokenHash, CancellationToken cancellationToken = default) => Task.FromResult<Quote?>(null);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class CompanyRepositoryProfileStub(CompanyQuotePdfProfile profile) : ICompanyRepository
    {
        public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Company?>(null);
        public Task<Company?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult<Company?>(null);
        public Task<CompanyQuotePdfProfile?> GetQuotePdfProfileAsync(Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<CompanyQuotePdfProfile?>(companyId == profile.CompanyId ? profile : null);
        public Task AddAsync(Company company, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CustomerRepositoryStub(Customer customer) : ICustomerRepository
    {
        public Task<bool> AreLocationsValidAsync(IReadOnlyCollection<CustomerAddressInput> addresses, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task AddAsync(Customer value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> DocumentExistsAsync(Guid companyId, string normalizedDocument, Guid? excludedCustomerId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Customer>> GetAllForCompanyAsync(Guid companyId, GetCustomersQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Customer>>([]);
        public Task<Customer?> GetForCompanyAsync(Guid customerId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<Customer?>(customerId == customer.Id && companyId == customer.CompanyId ? customer : null);
        public Task<Customer?> GetByDocumentAsync(Guid companyId, string normalizedDocument, CancellationToken cancellationToken = default) => Task.FromResult<Customer?>(null);
        public void RemoveAddresses(Customer customer) { }
        public void RemoveAddress(CustomerAddress address) { }
        public void Remove(Customer customer) { }
        public Task<CustomerPersistenceOutcome> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(CustomerPersistenceOutcome.Success);
    }

    private sealed class UnavailableFileStorage : IFileStorage
    {
        public Task<FileStorageUploadResult> UploadAsync(FileStorageUploadRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FileStorageDownloadResult> GetAsync(string key, CancellationToken cancellationToken) => throw new HttpRequestException();
        public Task DeleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
