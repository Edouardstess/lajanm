# Architecture

## Stack

| Component | Choice | Why |
|---|---|---|
| Mobile app (iOS + Android) | React Native (Expo) | Single codebase, mature biometrics/notifications/QR/secure-storage support, OTA updates for fast fixes |
| Back-office web | Next.js | Shares the team's React skills, good SSR support for an internal dashboard |
| Backend API | NestJS (TypeScript) | Modular structure suited to a financial system (auth/KYC/ledger/payments/fraud/admin as separate modules), good ecosystem for queues/retries |
| Database | PostgreSQL | Strict ACID — required for a double-entry financial ledger |
| Cache / queues | Redis + BullMQ | Retry/backoff for MonCash/NatCash calls, rate limiting, sessions |
| Mobile local storage | Encrypted SQLite/MMKV (added when the offline balance/history feature ships) | Offline balance/history consultation without exposing data if the phone is lost |
| Infra | Docker, GitHub Actions CI/CD, separate dev/staging/prod environments | NF-24 |

## The ledger (core invariant)

The single most sensitive part of the system is how balances are tracked.
`services/api/src/modules/ledger` implements a **double-entry ledger**:

- Every financial movement is one `Operation` with two or more balanced
  `LedgerEntry` rows (debits and credits summing to zero per currency —
  enforced in `LedgerService.assertBalanced`, not just assumed).
- Every `Operation` carries a **mandatory, unique `idempotencyKey`**
  (`operations.idempotencyKey`, unique-constrained at the database level).
  A retried request — e.g. MonCash resending the same webhook — can never
  create a second movement; `LedgerService.postOperation` detects the
  resulting unique-violation and returns the original result with
  `idempotent: true` instead of erroring.
- A user's balance is **never a stored, mutable counter**. It is always
  derived as `SUM(credits) - SUM(debits)` from `ledger_entries`
  (`LedgerService.getBalance`). This makes the balance trivially
  reconcilable against the sum of its history.
- `ledger_entries` rows are **append-only**: once written, a row is never
  updated or deleted. A mistake (e.g. a payout that gets rejected by
  MonCash after funds were reserved) is corrected by posting a new,
  balanced reversing `Operation` (`LedgerService.reverseOperation`), never
  by mutating the original entries. This is enforced twice — the
  application layer never issues an UPDATE/DELETE against
  `ledger_entries`, and a database trigger
  (`AddImmutabilityTriggers` migration) rejects any UPDATE/DELETE
  statement against the table outright, so even a bug or an ad-hoc admin
  query cannot rewrite history.

`accounts` models both user wallets (`ownerType = 'user'`) and external
rails (`ownerType = 'system'`, e.g. a `moncash_float` account). This means
money entering or leaving Lajan'm through MonCash/NatCash is *still* a
balanced double entry against a system account, not a bare credit/debit —
the same invariant applies uniformly everywhere.

## Audit log

`audit_logs` (`services/api/src/modules/audit`) is a second append-only
table, covering every sensitive action across every module (login, PIN
change, KYC decision, admin action, ...), not just ledger movements. Same
two-layer immutability: `AuditService` exposes only `record`/`find*`
methods, and the same kind of database trigger blocks UPDATE/DELETE.

## Module boundaries

`services/api/src/modules/*` mirrors the phased plan: `auth`, `kyc`,
`ledger`, `topup`, `payout`, `wallet`, `security`, `fraud`, `compliance`,
`support`. Every module beyond `ledger` and `audit` is currently a
placeholder (wired into `AppModule`, exposing a `_status` endpoint) —
business logic lands module by module in the phases that follow.

## Environments

`services/api/src/config/env.validation.ts` validates `NODE_ENV`, `PORT`,
`DATABASE_URL`, `REDIS_URL`, and `JWT_SECRET` at boot via `class-validator`;
the app refuses to start if any are missing or malformed. There is no
fallback or shared default across environments — dev, staging, and
production each supply their own values, dev via
`services/api/.env` (see `.env.development.example`), staging/production
via the deployment pipeline's secrets manager (see `.env.staging.example`
and `.env.production.example`, which are templates only).

## Regulatory-driven technical requirements

Some requirements from the legal analysis are deliberately built in from
the MVP rather than retrofitted later, because retrofitting them onto a
live financial system is much harder:

- **Per-transaction cap (100,000 HTG)**: to be enforced in the `payout`
  module as a configurable limit (not hardcoded), active by default.
- **Fund segregation / reconciliation**: the `accounts` model already
  separates user wallets from system float accounts so the `compliance`
  module can reconcile the aggregate MonCash/NatCash float against the sum
  of user balances derived from the ledger.
- **Immutable data retention**: covered by the append-only `ledger_entries`
  and `audit_logs` design above.
- **UCREF reporting**: the `compliance` module will provide internal
  case documentation tooling (not automatic transmission to UCREF).
