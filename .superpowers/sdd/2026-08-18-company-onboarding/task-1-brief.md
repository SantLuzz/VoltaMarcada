### Task 1: Create the domain tenant and membership model

**Files:**
- Create: `src/ServiceSync.Domain/Entities/CompanyUser.cs`
- Create: `src/ServiceSync.Domain/Enums/CompanyUserRole.cs`
- Create: `src/ServiceSync.Domain/ValueObjects/BrazilianDocument.cs`
- Modify: `src/ServiceSync.Domain/Entities/Company.cs`

**Produces:**

```csharp
public static Company Create(string legalName, string tradeName, string document,
    string phone, string whatsApp, string city, string state, string? logoKey,
    string slug, DateTimeOffset now);
public static CompanyUser Create(Guid userId, Guid companyId,
    CompanyUserRole role, DateTimeOffset now);
public static bool BrazilianDocument.TryNormalize(string? value, out string normalizedDocument);
```

- [ ] Add `CompanyUserRole` with `Owner = 1` and `Employee = 2`, then add a membership entity with private setters/EF constructor and invariant validation for non-empty IDs and defined role.
- [ ] Implement `BrazilianDocument.TryNormalize`: retain digits only, reject repeated sequences, and validate either CPF or CNPJ check digits without infrastructure dependencies.
- [ ] Expand `Company` to `LegalName`, `TradeName`, `Document`, `Phone`, `WhatsApp`, `City`, `State`, optional `LogoKey`, `Slug`, `CreatedAtUtc`, and `UpdatedAtUtc`. Trim text, require all non-logo values, require a normalized valid document, and assign timestamps from `now`.
- [ ] Run `dotnet build ServiceSync.sln`; expect exit code 0.
