# Final Fix Report

## Scope

Implemented the two requested final-review corrections only:

- Aligned customer command validation maximum lengths with the current EF Core mappings.
- Changed the successful customer-create `Location` header to the mapped edit endpoint.

Deferred aggregate encapsulation observations were intentionally left unchanged.

## Root cause

The create and update validators checked required fields, Brazilian document and postal-code formats, address validity, and the one-primary-address rule, but did not enforce the EF Core string lengths. An overlong request could therefore pass application validation and fail during persistence instead of returning `InvalidInput`.

The create endpoint used `/customers/{id}` in `Results.Created`, but the endpoint map exposes the individual customer resource at `/customers/{id}/edit`.

## Changes

| Area | Change | Persistence contract |
| --- | --- | --- |
| Create customer validation | Added `MaximumLength` rules for `Name`, `WhatsApp`, `Email`, and `Notes`. | 200, 20, 320, and 2000 |
| Shared address validation | Added `MaximumLength` rules for `Street`, `Number`, and `Complement`. This validator is used by both create and update commands. | 200, 50, and 200 |
| Update customer validation | Added the same customer field `MaximumLength` rules as create. | 200, 20, 320, and 2000 |
| Create endpoint | Changed `Location` from `/customers/{id}` to `/customers/{id}/edit`. | Matches `MapGet("/{customerId:guid}/edit", GetForEditAsync)` |

## Static verification

No build, test, or application command was run, per the task and project instructions.

- Reviewed `CustomerConfiguration` and `CustomerAddressConfiguration`: all seven validator bounds exactly match `HasMaxLength` mappings.
- Confirmed `CustomerAddressInputValidator` remains applied from both create and update validators through `RuleForEach`.
- Confirmed existing document, CEP, enum, location, non-empty-address, one-primary-address, tenant flow, and operation-result handling remain intact.
- Confirmed `CustomerEndpoints` has no raw endpoint exception path and the returned create location maps to its existing individual GET route.
- Checked changed files for whitespace-only edits and limited staging to the two validators, customer endpoint, and this report.

## Worktree hygiene

The pre-existing Cloudinary and Company Address changes were not edited or staged.
