# Professional Availability Design

## Goal

Allow a company owner to configure a professional's standard weekly working schedule and register exceptional periods when that professional is unavailable, while keeping the employee's own view read-only.

## Scope

The change extends the existing Professionals module with:

- a single standard weekly schedule per professional;
- selection of the weekdays on which the professional works;
- standard entry, lunch start, lunch end, and exit times shared by every selected weekday;
- exceptional unavailability records for time off, vacations, appointments, training, absences, and manual blocks;
- full-day date ranges and partial-day time ranges;
- owner management and employee read-only consultation on the existing professional screen.

Appointment slot calculation, appointment collision validation, company timezone configuration, public booking, calendar visualization, and recurring schedules with different hours per weekday remain outside this change.

## User Experience

The professional registration page keeps the existing create flow focused on selecting the employee and services. Availability is configured only after the professional has been created.

When editing an existing professional, the page presents two MudBlazor tabs:

1. **Dados e serviços** contains the existing employee and services form.
2. **Disponibilidade** contains the weekly schedule and exceptional unavailability management.

The availability tab shows a weekly schedule card with:

- selectable chips or checkboxes for Monday through Sunday;
- entry time;
- lunch start time;
- lunch end time;
- exit time;
- a dedicated save action and loading, validation, error, and success feedback.

The same four times apply to every selected workday. This deliberately minimizes data entry for the initial target users. A professional must have at least one selected workday when a weekly schedule is saved.

Below the weekly schedule, an unavailability section lists existing records and offers an **Adicionar indisponibilidade** action. The form uses MudBlazor controls for:

- type: Time off, Vacation, Appointment, Training, Absence, or Manual block;
- start date;
- end date;
- full-day selection;
- start time and end time when the record is not full-day;
- optional notes.

A one-day absence uses the same date for start and end. Vacation normally uses a multi-day full-day range. Timed unavailability is limited to a single date in this version; multi-day records are full-day ranges. The list displays the type, human-readable period, notes, and a remove action with confirmation.

On creation, the availability tab is hidden and a short explanation states that availability can be configured after the professional is saved. On editing, the Company Owner can manage the schedule and exceptions. An Employee viewing their linked professional can see the same information without mutation actions or enabled fields.

## Domain Model

### ProfessionalAvailability

`ProfessionalAvailability` is a company-scoped entity with server-generated `Guid` identity and a one-to-one relationship with `Professional`. It stores:

- `Id`;
- `CompanyId`;
- `ProfessionalId`;
- selected weekdays as a flags enum;
- `EntryTime`;
- `LunchStartTime`;
- `LunchEndTime`;
- `ExitTime`;
- `CreatedAtUtc`;
- `UpdatedAtUtc`.

The selected weekdays enum uses English source identifiers and supports Monday through Sunday. Time values use `TimeOnly` because they represent local business clock times rather than instants.

The entity enforces:

- at least one supported weekday is selected;
- `EntryTime` is earlier than `LunchStartTime`;
- `LunchStartTime` is earlier than `LunchEndTime`;
- `LunchEndTime` is earlier than `ExitTime`;
- company and professional identifiers are not empty.

Saving the weekly schedule creates it when absent or replaces the existing weekday and time values when present. There is at most one availability record per professional.

### ScheduleBlock

`ScheduleBlock` is a company-scoped entity with server-generated `Guid` identity. It represents an exceptional unavailability and stores:

- `Id`;
- `CompanyId`;
- `ProfessionalId`;
- `ScheduleBlockType`;
- `StartDate`;
- `EndDate`;
- `IsFullDay`;
- nullable `StartTime`;
- nullable `EndTime`;
- optional `Notes`;
- `CreatedAtUtc`.

`ScheduleBlockType` contains English identifiers equivalent to Time off, Vacation, Appointment, Training, Absence, and Manual block. UI labels remain in Brazilian Portuguese.

The entity enforces:

- valid company and professional identifiers;
- `EndDate` is not earlier than `StartDate`;
- full-day blocks do not store start or end times;
- partial-day blocks require both times, require `StartTime` before `EndTime`, and require the same start and end date;
- notes are normalized and length-limited.

Schedule blocks are removed physically because they are configuration exceptions with no historical appointment or work-order relationship in this phase. Removing a block does not affect the professional or any other tenant data.

## Application Behavior

The feature follows the existing explicit-use-case and repository patterns:

- `GetProfessionalAvailabilityUseCase` loads the weekly schedule and blocks for an existing current-company professional;
- `SaveProfessionalAvailabilityUseCase` creates or updates the weekly schedule;
- `CreateScheduleBlockUseCase` adds an exceptional unavailability;
- `DeleteScheduleBlockUseCase` removes an exceptional unavailability.

Commands and DTOs use `DateOnly`, `TimeOnly`, and the domain enums directly where appropriate. FluentValidation handles command shape and user-facing validation boundaries; domain entities protect their invariants independently.

Every use case resolves `CompanyId` from `ICurrentCompany`. Owner-only mutations reuse `ProfessionalOwnerAuthorization`. Repositories load a professional or child record by both its ID and `CompanyId`; no authenticated operation accepts a browser-supplied company identifier.

Expected failures use explicit operation result codes for invalid input, professional not found, schedule block not found, and forbidden access. Unexpected failures are logged by the page and produce localized generic feedback.

## Persistence

EF Core maps each significant entity in its own configuration class.

`ProfessionalAvailabilities` has:

- a composite alternate key compatible with tenant-scoped relationships;
- a unique index on `CompanyId` and `ProfessionalId`;
- a restrictive tenant-scoped foreign key to `Professional`;
- PostgreSQL-compatible mappings for `TimeOnly` values and the weekday flags value.

`ScheduleBlocks` has:

- a tenant-scoped foreign key to `Professional`;
- an index on `CompanyId`, `ProfessionalId`, `StartDate`, and `EndDate`, matching future availability lookups;
- length limits for notes;
- PostgreSQL-compatible mappings for `DateOnly` and nullable `TimeOnly` values.

A descriptive migration adds both tables, constraints, relationships, and indexes. The existing `Professional` table and records require no data backfill because availability is optional until explicitly configured.

## Validation and Conflict Rules

The weekly schedule validates only internal time ordering. Schedule blocks validate their own date and time ranges.

Overlapping schedule blocks are allowed in this phase. Two real events can legitimately overlap in description, and blocking the overlap adds user friction without improving future availability calculations: any matching block makes the professional unavailable. The future slot-calculation use case will combine the standard weekly schedule with all applicable blocks.

The feature does not infer availability from the professional's active status. A deactivated professional retains their schedule and blocks for later reactivation, but operational selection already excludes inactive professionals through existing status rules.

## Security and Authorization

- The Company Owner can create or update the weekly schedule and create or remove schedule blocks.
- An Employee can read only the availability attached to their own linked professional record.
- All reads, writes, and deletes filter by `CompanyId` at query time.
- Child records use tenant-scoped foreign keys so cross-company relationships cannot be persisted.
- UI authorization is convenience only; application use cases and HTTP endpoints enforce owner-only mutation.
- Internal identifiers are not treated as authorization credentials.

## Verification

No automated tests are added because this is MVP validation work and tests were not requested. No build, test run, application launch, or migration execution occurs unless explicitly requested.

Validation is static and includes:

- domain invariant review for schedule and block ranges;
- tenant-scoped repository predicate review;
- authorization review for Owner and Employee paths;
- EF Core relationship, uniqueness, index, and migration-intent review;
- command, DTO, and result consistency review;
- Razor component review for MudBlazor usage, responsive layout, accessible labels, loading states, validation, confirmations, and localized feedback;
- review that existing uncommitted workspace changes are preserved.

## Assumptions

- Every selected workday uses the same entry, lunch, and exit times in the MVP.
- Lunch is mandatory for a saved weekly schedule because the approved UI explicitly captures its start and end.
- Full-day date ranges cover every selected calendar date inclusively.
- Partial-day unavailability is restricted to one calendar date.
- Company scheduling is expressed in local business dates and times until company timezone configuration and appointment instants are implemented.
- Availability remains optional, allowing existing professionals to continue without a configured schedule.
