# Customer Registration Backend Design

## Goal

Add the phase-two customer registration backend so a company can create, read, edit, update, and delete its own customers and their service addresses. The design must support customers created manually now and customers originated by booking requests in a future phase.

## Scope

This change includes Domain, Application, Infrastructure, and authenticated HTTP endpoints. It does not include Razor pages, public booking integration, work orders, or automated tests.

## Domain Model

`Customer` is a company-scoped aggregate. It contains:

- `Id`
- `CompanyId`
- `Name`
- `Document` (required CPF or CNPJ, normalized)
- `WhatsApp` (required contact number)
- `Email` (optional)
- `Notes` (optional)
- `CreatedAtUtc`
- `UpdatedAtUtc`
- a private collection of `CustomerAddress`

`CustomerAddress` belongs to exactly one `Customer` and contains:

- `Id`
- `CustomerId`
- `AddressType`
- `IsPrimary`
- `Street`
- `Number`
- `PostalCode`
- `Complement` (optional)
- `StateId`
- `CityId`

`CustomerAddressType` is an English source-code enum with `Residence`, `Business`, `BeachHouse`, and `Other`. UI labels can later be localized to Brazilian Portuguese.

The aggregate requires at least one address and exactly one primary address. It validates required textual data and normalizes CPF/CNPJ and CEP using the existing Brazilian value-object helpers. Application logic validates every requested city belongs to its submitted state before persistence.

## Application Flow

Dedicated use cases provide the CRUD behavior:

- `CreateCustomerUseCase`
- `GetCustomersUseCase`
- `GetCustomerForEditUseCase`
- `UpdateCustomerUseCase`
- `DeleteCustomerUseCase`

Each mutation resolves the current company through `ICurrentCompany`; the request contract never supplies a `CompanyId`.

Create receives the complete customer aggregate input. Update loads the customer using both the requested customer ID and the current company ID. It removes all existing addresses, updates the customer details, and inserts the submitted address collection. Delete also loads using customer ID and current company ID, then deletes the aggregate and its addresses.

`GetCustomers` filters exclusively by the current company and returns every customer with every address. `GetCustomerForEdit` filters by both customer ID and current company ID and returns the complete customer aggregate. All reads return dedicated immutable DTOs rather than EF Core entities.

## HTTP API

The authenticated company-member API exposes:

- `POST /customers`
- `GET /customers`
- `GET /customers/{customerId}/edit`
- `PUT /customers/{customerId}`
- `DELETE /customers/{customerId}`

Every endpoint requires the existing company-member authorization policy. Result contracts distinguish invalid input from a customer not found in the current company. A resource that exists in another company is indistinguishable from a missing resource.

## Persistence

EF Core configurations add `Customers` and `CustomerAddresses` tables. Customer addresses have a required foreign key to customers with cascade deletion, and required foreign keys to the existing `States` and `Cities` reference tables with restrictive deletion.

Indexes support company-scoped customer retrieval and customer-address retrieval. A filtered PostgreSQL unique index ensures no customer has more than one primary address; the domain enforces the matching exactly-one invariant before persistence.

A descriptive EF Core migration adds both tables and their constraints. No persistence query accepts a tenant identifier from HTTP input.

## Error Handling and Validation

FluentValidation validates command shapes and basic field constraints. Domain methods protect aggregate invariants. Use cases return explicit outcome codes for invalid input and missing customer conditions rather than using exceptions as expected control flow.

The web layer maps outcomes to appropriate HTTP responses and never exposes raw database or domain exception details.

## Verification

Per the current MVP project rule, this change adds no automated tests unless explicitly requested. It will be verified through static review of layering, route authorization, tenant filters, mappings, migration consistency, and request/response contracts. No build or application execution is run unless explicitly requested.
