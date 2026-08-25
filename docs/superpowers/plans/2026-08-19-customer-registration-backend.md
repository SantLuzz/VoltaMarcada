# Customer Registration Backend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Deliver company-isolated backend CRUD for the complete Customer aggregate, including one or more typed service addresses.

**Architecture:** Customer is a domain aggregate with a private address collection and rules for a valid document, one or more addresses, and exactly one primary address. Explicit application use cases resolve the tenant with ICurrentCompany, call a focused persistence abstraction, and return DTOs. Infrastructure maps the aggregate, while Web exposes protected JSON endpoints.

**Tech Stack:** .NET 10, C#, ASP.NET Core minimal APIs, EF Core 10, PostgreSQL, FluentValidation.

**Spec:** docs/superpowers/specs/2026-08-19-customer-registration-design.md

## Global Constraints

- Use English in identifiers, migration names, database objects, and technical messages.
- Resolve CompanyId only with ICurrentCompany. HTTP contracts do not contain CompanyId.
- Get-for-edit, update, and delete query by both Customer.Id and Customer.CompanyId.
- Document and WhatsApp are required. Email, Notes, and Complement are optional.
- Customers require one or more addresses and exactly one primary address.
- Use BrazilianDocument, BrazilianPostalCode, States, and Cities. Validate each city/state pair.
- Update removes every existing address row and inserts the submitted collection in one save operation.
- Both read operations return immutable DTOs containing all address values. Do not return entities.
- Every endpoint requires AuthorizationPolicies.CompanyMember.
- Do not add automated tests or run build, tests, or application. Validate by static inspection only.
- Preserve user changes. Stage only files from this feature.

---

## File Structure

| File | Responsibility |
| --- | --- |
| src/VoltaMarcada.Domain/Enums/CustomerAddressType.cs | Address-category enum. |
| src/VoltaMarcada.Domain/Entities/Customer.cs | Company-scoped aggregate and invariants. |
| src/VoltaMarcada.Domain/Entities/CustomerAddress.cs | Address entity and field validation. |
| src/VoltaMarcada.Application/Customers | Commands, DTOs, validators, results, and CRUD use cases. |
| src/VoltaMarcada.Application/Abstractions/Persistence/ICustomerRepository.cs | Customer persistence boundary. |
| src/VoltaMarcada.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs | Customer mapping. |
| src/VoltaMarcada.Infrastructure/Persistence/Configurations/CustomerAddressConfiguration.cs | Address mapping and constraints. |
| src/VoltaMarcada.Infrastructure/Persistence/Repositories/CustomerRepository.cs | Scoped aggregate persistence. |
| src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260819110000_AddCustomers.cs | Schema migration. |
| src/VoltaMarcada.Web/Endpoints/CustomerEndpoints.cs | Protected JSON API. |

### Task 1: Add Customer Domain Types

**Files:**
- Create: src/VoltaMarcada.Domain/Enums/CustomerAddressType.cs
- Create: src/VoltaMarcada.Domain/Entities/Customer.cs
- Create: src/VoltaMarcada.Domain/Entities/CustomerAddress.cs

**Interfaces:**
- Consumes: ICompanyScopedEntity, DomainException, BrazilianDocument, and BrazilianPostalCode.
- Produces: Customer, CustomerAddress, and CustomerAddressType.

- [ ] **Step 1: Define the address category enum.**

~~~
public enum CustomerAddressType
{
    Residence,
    Business,
    BeachHouse,
    Other
}
~~~

- [ ] **Step 2: Implement validated address construction.**

~~~
public static CustomerAddress Create(
    CustomerAddressType addressType,
    bool isPrimary,
    string street,
    string number,
    string postalCode,
    string? complement,
    Guid stateId,
    Guid cityId)
~~~

Require a defined enum value, non-empty street and number, non-empty location IDs, normalized CEP, and a trimmed optional complement.

- [ ] **Step 3: Implement the company-scoped Customer aggregate.**

~~~
public static Customer Create(
    Guid companyId,
    string name,
    string document,
    string whatsApp,
    string? email,
    string? notes,
    IEnumerable<CustomerAddress> addresses,
    DateTimeOffset now);

public void Update(
    string name,
    string document,
    string whatsApp,
    string? email,
    string? notes,
    DateTimeOffset now);

public void ReplaceAddresses(IEnumerable<CustomerAddress> addresses);
~~~

Store addresses in a private list exposed as IReadOnlyCollection<CustomerAddress>. CustomerAddress also exposes CustomerId, State, and City with private setters so EF Core can materialize the required relationships and DTO mapping can return the location names.

- [ ] **Step 4: Enforce aggregate invariants before construction and replacement.** Require non-empty company ID, valid normalized document, name, WhatsApp, at least one address, and exactly one primary address. Update UpdatedAtUtc with now.ToUniversalTime.

- [ ] **Step 5: Statically inspect domain boundaries.** Confirm private setters and no EF Core, ASP.NET Core, Application, Infrastructure, or Web dependency.

- [ ] **Step 6: Commit only the domain files.**

~~~
git add -- src/VoltaMarcada.Domain/Enums/CustomerAddressType.cs src/VoltaMarcada.Domain/Entities/Customer.cs src/VoltaMarcada.Domain/Entities/CustomerAddress.cs
git commit -m "feat: add customer aggregate"
~~~

### Task 2: Add Application Contracts and CRUD Use Cases

**Files:**
- Create: src/VoltaMarcada.Application/Customers/CustomerAddressInput.cs
- Create: src/VoltaMarcada.Application/Customers/CreateCustomerCommand.cs
- Create: src/VoltaMarcada.Application/Customers/UpdateCustomerCommand.cs
- Create: src/VoltaMarcada.Application/Customers/CreateCustomerCommandValidator.cs
- Create: src/VoltaMarcada.Application/Customers/UpdateCustomerCommandValidator.cs
- Create: src/VoltaMarcada.Application/Customers/CustomerAddressDto.cs
- Create: src/VoltaMarcada.Application/Customers/CustomerDto.cs
- Create: src/VoltaMarcada.Application/Customers/CustomerOperationResult.cs
- Create: src/VoltaMarcada.Application/Customers/CreateCustomerUseCase.cs
- Create: src/VoltaMarcada.Application/Customers/GetCustomersUseCase.cs
- Create: src/VoltaMarcada.Application/Customers/GetCustomerForEditUseCase.cs
- Create: src/VoltaMarcada.Application/Customers/UpdateCustomerUseCase.cs
- Create: src/VoltaMarcada.Application/Customers/DeleteCustomerUseCase.cs
- Create: src/VoltaMarcada.Application/Abstractions/Persistence/ICustomerRepository.cs
- Modify: src/VoltaMarcada.Application/DependencyInjection.cs

**Interfaces:**
- Consumes: Task 1 types, ICurrentCompany, TimeProvider, and FluentValidation.
- Produces: Web-consumable use cases and an Infrastructure persistence interface.

- [ ] **Step 1: Define immutable input and full output contracts.**

~~~
public sealed record CustomerAddressInput(
    CustomerAddressType AddressType,
    bool IsPrimary,
    string Street,
    string Number,
    string PostalCode,
    string? Complement,
    Guid StateId,
    Guid CityId);

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Document,
    string WhatsApp,
    string? Email,
    string? Notes,
    IReadOnlyList<CustomerAddressDto> Addresses);
~~~

CustomerAddressDto includes its ID, type, primary status, street, number, postal code, complement, state ID/name/abbreviation, and city ID/name.

- [ ] **Step 2: Define ICustomerRepository.**

~~~
public interface ICustomerRepository
{
    Task<bool> AreLocationsValidAsync(IReadOnlyCollection<CustomerAddressInput> addresses, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> GetAllForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Customer?> GetForCompanyAsync(Guid customerId, Guid companyId, CancellationToken cancellationToken = default);
    void RemoveAddresses(Customer customer);
    void Remove(Customer customer);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
~~~

- [ ] **Step 3: Implement both validators.** Require document and WhatsApp; allow missing email/complement; validate document and postal code with existing helpers; require a non-empty address collection, valid location IDs, and exactly one primary input.

- [ ] **Step 4: Implement create and update use cases.** Resolve company through ICurrentCompany, validate command and all submitted locations, transform inputs to domain addresses, and use TimeProvider.GetUtcNow. Update applies the required scoped lookup and full address replacement.

~~~
var customer = await repository.GetForCompanyAsync(customerId, companyId, cancellationToken);
if (customer is null)
{
    return CustomerOperationResult.NotFound();
}

repository.RemoveAddresses(customer);
customer.Update(command.Name, command.Document, command.WhatsApp, command.Email, command.Notes, now);
customer.ReplaceAddresses(addresses);
await repository.SaveChangesAsync(cancellationToken);
~~~

- [ ] **Step 5: Implement get and delete use cases.** Get all maps every scoped aggregate to full CustomerDto. Get-for-edit and delete use GetForCompanyAsync. Delete removes the tracked aggregate and saves.

- [ ] **Step 6: Define explicit results.** CustomerOperationResultCode contains Success, InvalidInput, InvalidLocation, and NotFound. Expected validation and missing-resource outcomes do not use exceptions.

- [ ] **Step 7: Register use cases in AddApplication.**

~~~
services.AddScoped<CreateCustomerUseCase>();
services.AddScoped<GetCustomersUseCase>();
services.AddScoped<GetCustomerForEditUseCase>();
services.AddScoped<UpdateCustomerUseCase>();
services.AddScoped<DeleteCustomerUseCase>();
~~~

- [ ] **Step 8: Statically inspect Application boundaries.** Confirm no file imports Infrastructure, Web, EF Core, or HTTP types, and no command accepts CompanyId.

- [ ] **Step 9: Commit only Application customer files, repository abstraction, and DI registration.**

~~~
git add -- src/VoltaMarcada.Application/Customers src/VoltaMarcada.Application/Abstractions/Persistence/ICustomerRepository.cs src/VoltaMarcada.Application/DependencyInjection.cs
git commit -m "feat: add customer use cases"
~~~

### Task 3: Map and Persist the Aggregate

**Files:**
- Create: src/VoltaMarcada.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Configurations/CustomerAddressConfiguration.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Repositories/CustomerRepository.cs
- Modify: src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs
- Modify: src/VoltaMarcada.Infrastructure/DependencyInjection.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260819110000_AddCustomers.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260819110000_AddCustomers.Designer.cs
- Modify: src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs

**Interfaces:**
- Consumes: ICustomerRepository, domain aggregate types, and Company, State, City reference tables.
- Produces: scoped PostgreSQL persistence.

- [ ] **Step 1: Add Customer and CustomerAddress DbSets to AppDbContext.**

~~~
public DbSet<Customer> Customers => Set<Customer>();
public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
~~~

- [ ] **Step 2: Configure Customers.** Use Customers table, non-generated GUID ID, required company/name/document/WhatsApp/timestamps, nullable email/notes, restrictive Company foreign key, private address navigation, and IX_Customers_CompanyId_Name over CompanyId and Name.

- [ ] **Step 3: Configure CustomerAddresses.** Use CustomerAddresses table, required fields, Customer cascade deletion, restrictive State and City deletion, IX_CustomerAddresses_CustomerId, and filtered unique index to prevent two primary addresses.

~~~
builder.HasIndex(address => new { address.CustomerId, address.IsPrimary })
    .HasDatabaseName("UX_CustomerAddresses_CustomerId_Primary")
    .IsUnique()
    .HasFilter("\"IsPrimary\" = true");
~~~

- [ ] **Step 4: Implement CustomerRepository.** Get methods include addresses, State, and City. GetForCompanyAsync uses the compound database predicate. AreLocationsValidAsync returns true only when every distinct submitted city/state pair exists. RemoveAddresses calls RemoveRange on tracked customer addresses.

~~~
return await dbContext.Customers
    .Include(customer => customer.Addresses)
    .ThenInclude(address => address.State)
    .Include(customer => customer.Addresses)
    .ThenInclude(address => address.City)
    .SingleOrDefaultAsync(
        customer => customer.Id == customerId && customer.CompanyId == companyId,
        cancellationToken);
~~~

- [ ] **Step 5: Register ICustomerRepository.**

~~~
services.AddScoped<ICustomerRepository, CustomerRepository>();
~~~

- [ ] **Step 6: Author AddCustomers migration, designer metadata, and model snapshot.** Add both tables with every column, foreign key, index, and filtered primary-address constraint above, retaining the existing PascalCase database convention.

- [ ] **Step 7: Compare entity properties, configurations, migration, and snapshot statically.** Fix every mismatch in lengths, nullability, keys, indexes, and delete behavior.

- [ ] **Step 8: Commit only Infrastructure files in this task.**

~~~
git add -- src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs src/VoltaMarcada.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs src/VoltaMarcada.Infrastructure/Persistence/Configurations/CustomerAddressConfiguration.cs src/VoltaMarcada.Infrastructure/Persistence/Repositories/CustomerRepository.cs src/VoltaMarcada.Infrastructure/DependencyInjection.cs src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260819110000_AddCustomers.cs src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260819110000_AddCustomers.Designer.cs src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs
git commit -m "feat: persist customers and addresses"
~~~

### Task 4: Expose Protected Customer Endpoints

**Files:**
- Create: src/VoltaMarcada.Web/Endpoints/CustomerEndpoints.cs
- Modify: src/VoltaMarcada.Web/Program.cs

**Interfaces:**
- Consumes: all five customer use cases, complete DTOs, result codes, and AuthorizationPolicies.CompanyMember.
- Produces: JSON endpoints at /customers.

- [ ] **Step 1: Define request records without CompanyId.**

~~~
public sealed record CustomerRequest(
    string Name,
    string Document,
    string WhatsApp,
    string? Email,
    string? Notes,
    IReadOnlyList<CustomerAddressRequest> Addresses);

public sealed record CustomerAddressRequest(
    CustomerAddressType AddressType,
    bool IsPrimary,
    string Street,
    string Number,
    string PostalCode,
    string? Complement,
    Guid StateId,
    Guid CityId);
~~~

- [ ] **Step 2: Map the protected route group.**

~~~
var customers = endpoints.MapGroup("/customers")
    .RequireAuthorization(AuthorizationPolicies.CompanyMember);

customers.MapPost("/", CreateAsync);
customers.MapGet("/", GetAllAsync);
customers.MapGet("/{customerId:guid}/edit", GetForEditAsync);
customers.MapPut("/{customerId:guid}", UpdateAsync);
customers.MapDelete("/{customerId:guid}", DeleteAsync);
~~~

- [ ] **Step 3: Implement creation and update.** Map the request to the corresponding command. Return 201 Created after creation and 200 OK after update. Map InvalidInput and InvalidLocation to 400 with fixed Portuguese user messages, never exception or database text.

- [ ] **Step 4: Implement full reads and deletion.** Get all returns every complete CustomerDto for the current company. Get-for-edit returns full CustomerDto or 404. Delete returns 204 or 404.

- [ ] **Step 5: Map the endpoints in Program.cs.**

~~~
app.MapCustomerEndpoints();
~~~

- [ ] **Step 6: Statically inspect API security.** Verify every route inherits the policy, request contracts omit CompanyId, endpoint code does not use AppDbContext, and output is DTO-only.

- [ ] **Step 7: Commit only CustomerEndpoints.cs and Program.cs.**

~~~
git add -- src/VoltaMarcada.Web/Endpoints/CustomerEndpoints.cs src/VoltaMarcada.Web/Program.cs
git commit -m "feat: expose customer api"
~~~

### Task 5: Final Static Verification

**Files:**
- Review: every file changed in Tasks 1 through 4.

**Interfaces:**
- Consumes: all completed Customer implementation layers.
- Produces: evidence-backed handoff without execution.

- [ ] **Step 1: Check whitespace and forbidden stubs.**

~~~
git diff --check
rg -n "TODO|TBD|NotImplementedException" src/VoltaMarcada.Domain src/VoltaMarcada.Application/Customers src/VoltaMarcada.Infrastructure/Persistence src/VoltaMarcada.Web/Endpoints/CustomerEndpoints.cs
~~~

- [ ] **Step 2: Trace tenant isolation.** From each route to each repository query, confirm server-side company resolution and compound customer/company filtering for individual operations.

- [ ] **Step 3: Trace aggregate replacement and response completeness.** Confirm update removes old address rows before replacement and both read paths map every address field including type, primary marker, city, and state.

- [ ] **Step 4: Reconcile schema artifacts.** Confirm migration and snapshot match entity configurations for both new tables, all foreign keys, indexes, nullability, and cascade/restrict behavior.

- [ ] **Step 5: Report static-review evidence and state explicitly that build, automated tests, and application execution were not run under the project rule.**
