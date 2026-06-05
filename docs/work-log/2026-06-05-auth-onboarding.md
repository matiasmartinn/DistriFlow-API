# Authentication and onboarding work log

Date: 2026-06-05

## Implemented

- Public `POST /api/distributors` onboarding creates one `Distributor` and its
  initial `User` with the `Administrator` role.
- The administrator password is hashed with BCrypt before persistence.
- Onboarding adds both aggregates and completes persistence with one
  `SaveChangesAsync`, keeping the operation in the same transaction.
- Public JWT login authenticates an existing user and does not create a
  distributor or user.
- Login validates that both the user and distributor are active.
- Issued JWTs contain the `userId`, `distributorId`, and `role` claims.
- `CurrentUserContext` reads the authenticated user's claims and exposes them
  to Application services.
- Tenant-owned endpoints obtain `DistributorId` from `CurrentUserContext`
  instead of accepting it in the request.
- Role authorization protects distributor suspend/reactivate operations for
  `Administrator`.
- EF Core migration `AddUserAuthentication` adds the persisted password hash.
- No refresh-token flow or complete User vertical was implemented.

## Verification

- `dotnet build DistriFlow-API.sln` completed with zero errors and zero
  warnings.
- EF Core reported no pending model changes after generating
  `AddUserAuthentication`.
- A request to a protected endpoint without a JWT returned `401`.
- An invalid login returned `401`.
- A `SalesRep` JWT attempting to suspend a distributor returned `403`.
- Onboarding without an administrator password returned `400`.

## Pending

- The migration was not applied because the configured PostgreSQL credentials
  are fictitious and cannot authenticate against the local server.
- Decide whether distributor suspend/reactivate operations remain available to
  the current tenant administrator or must belong to a future platform actor.
