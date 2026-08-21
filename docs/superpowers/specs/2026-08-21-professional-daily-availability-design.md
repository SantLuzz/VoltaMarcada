# Professional Daily Availability Design

## Goal

Allow a Company Owner to configure a professional's working hours independently for each weekday, including a reduced uninterrupted Saturday schedule, while retaining a quick way to copy one day's schedule to every other active day.

## Scope

This change replaces the existing shared weekly hours model. A professional has one optional availability aggregate with zero or more daily schedules, one per active weekday. Each daily schedule has a work period and an optional lunch interval.

The existing schedule-block feature, its authorization rules, and its records remain unchanged. Appointment slot calculation, collision detection, company time zones, multiple work intervals in a day, and public booking remain outside this change.

## User Experience

The existing availability panel replaces the **Horário padrão** section with **Horários de trabalho**. It renders the seven weekdays in Monday-to-Sunday order. Every row provides:

- an enabled checkbox;
- entry and exit time fields;
- a **Possui intervalo** toggle;
- lunch start and lunch end fields only when the interval toggle is enabled;
- an **Aplicar aos demais dias ativos** action.

Disabled weekdays do not require times and have disabled fields. The default unsaved state enables Monday through Friday with `08:00–12:00` and `13:00–18:00`; Saturday and Sunday start disabled. The owner can enable Saturday, turn off its interval, and set `08:00–12:00` without affecting weekday rows.

The copy action uses its row as the source and copies its entry, exit, and interval values to every other active row. It does not activate inactive weekdays and does not persist data by itself. Each copied row remains independently editable. A single **Salvar horários** action persists the complete weekly configuration.

On narrow screens each weekday renders as a stacked card; larger screens use a compact row layout. The read-only view shows each active weekday with its work periods, grouping consecutive weekdays only when their periods are identical.

## Domain Model

`ProfessionalAvailability` remains the company-scoped aggregate root with a one-to-one relationship to `Professional`. It retains its identifiers and audit timestamps, but no longer stores weekday flags or shared clock times. It owns a collection of `ProfessionalDailySchedule` entities.

`ProfessionalDailySchedule` stores:

- `Id`;
- `CompanyId`;
- `ProfessionalAvailabilityId`;
- `Weekday` as `DayOfWeek`;
- `EntryTime` and `ExitTime` as `TimeOnly`;
- nullable `LunchStartTime` and `LunchEndTime` as `TimeOnly`;
- the parent availability relationship.

The aggregate replaces its complete collection when updated. It rejects an empty collection, duplicate weekdays, unsupported weekday values, an entry time not earlier than exit time, or an incomplete or unordered lunch interval. When a lunch interval exists, the invariant is `EntryTime < LunchStartTime < LunchEndTime < ExitTime`.

## Application and API Behavior

`SaveProfessionalAvailabilityCommand` and its HTTP request contract contain an ordered collection of daily schedules rather than a flag value and four shared times. `ProfessionalAvailabilityDto` exposes the same collection through `DailyScheduleDto`. The save use case validates the command shape, resolves the current company, checks owner authorization, and replaces the aggregate's daily schedules atomically.

The copy action exists only in the Blazor form model; no copy-specific API or use case is needed. The server never trusts a client-supplied company identifier and continues to locate the professional and availability by identifier plus the current company.

Expected invalid inputs return the existing explicit invalid-input result. The form surfaces a localized error identifying the invalid weekday or the absence of any active weekday. Unexpected failures are logged with the professional identifier and return the existing generic localized feedback.

## Persistence and Migration

EF Core maps `ProfessionalDailySchedule` to a new `ProfessionalDailySchedules` table. It has a tenant-scoped foreign key to `ProfessionalAvailabilities` and a unique index on `(CompanyId, ProfessionalAvailabilityId, Weekday)`. `TimeOnly` fields map to PostgreSQL `time without time zone`; lunch fields are nullable.

A descriptive migration creates the new table, transforms every existing `ProfessionalAvailabilities` record into one daily row for each selected weekday using its existing times, and then removes `WorkDays`, `EntryTime`, `LunchStartTime`, `LunchEndTime`, and `ExitTime` from the parent table. Schedule-block tables and data are not modified. The migration is reversible by regrouping only when all daily rows have the same values; otherwise its down path must explicitly fail rather than silently discard distinct schedules.

## Security and Compatibility

Owner-only mutations and employee self-only read access remain as currently specified. All repositories, aggregate loads, and child mutations use company-scoped predicates and tenant-scoped foreign keys. Existing configured schedules retain their effective hours after migration; professionals with no availability remain unconfigured.

## Verification

No automated tests, build, application launch, or migration execution are performed unless explicitly requested. Static verification will review:

- daily-schedule domain invariants and nullable interval handling;
- command, DTO, endpoint, and use-case consistency;
- tenant predicates, authorization, and foreign-key configuration;
- migration expansion of each selected weekday and preservation of schedule blocks;
- Razor state transitions, responsive presentation, copy behavior, and Brazilian Portuguese user-facing feedback.

## Assumptions

- A daily schedule represents one uninterrupted work period with at most one optional lunch interval.
- The copy action targets all other currently active days, not inactive days.
- Times represent local business clock times until company time-zone support is introduced.
- A professional may still have no configured availability.
