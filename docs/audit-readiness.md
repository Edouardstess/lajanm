# Security audit readiness

What an external auditor should look at, what is genuinely in place, and —
more usefully — what is **not**. Every "not done" below is a deliberate,
known gap rather than an oversight, and none of them should be discovered
by the auditor rather than read here.

> **Blocking governance item.** Lajan'm holds a client balance on its own
> ledger, which puts it close to the Fournisseur de Services de Paiement
> (FSP) status defined by **BRH Circular n°121**. Technical work has
> proceeded in parallel with legal clarification, but **no production
> launch with real user funds may happen until that status is resolved.**
> The system is built to run in sandbox/staging with test accounts, and
> `.env.production.example` deliberately keeps `MONCASH_BASE_URL` pointed
> at a secrets-manager value with a comment that production MonCash
> credentials must not be enabled before then. An audit performed before
> that clarification is an audit of a system that is not yet permitted to
> take real money.

## 1. The money invariants

| Invariant | Where enforced | Proven by |
|---|---|---|
| Every movement is balanced (debits = credits per currency) | `LedgerService.assertBalanced` | `ledger-invariants.integration.spec.ts` — an unbalanced post is rejected with no partial write |
| A balance is derived, never a stored counter | `LedgerService.getBalance` (`SUM(credits) - SUM(debits)`) | `ledger.integration.spec.ts` |
| A retried request can never move money twice | Unique constraint on `operations.idempotencyKey` | Integration test posts the same key sequentially **and** 5x concurrently; exactly one operation, one balance change |
| Corrections never mutate history | `LedgerService.reverseOperation` posts mirrored entries | Integration test: balance nets to zero, original entries and operation still present, status `REVERSED` |
| `ledger_entries` / `audit_logs` are append-only | Application layer **and** a Postgres trigger (`AddImmutabilityTriggers`) | Integration tests issue raw UPDATE/DELETE and assert the database refuses them |

The concurrency test is the one worth reviewing closely: Postgres aborts
the entire transaction on a unique violation, so the loser of an
idempotency race must re-read the winner on a **fresh connection**. An
earlier implementation did this inside the aborted transaction; the test
was verified to fail against that implementation before being trusted.

## 2. Authentication and authorisation

- Two separate token spaces — customer (`{sub, phone}`) and admin
  (`{sub, email, role, type:'admin'}`) — sharing one `JWT_SECRET` and
  distinguished **only** by the `type` claim.
- **Both** guards check it. `JwtAuthGuard` rejecting admin tokens is a
  regression test (`jwt-auth.guard.spec.ts`) for a bug that was live: an
  admin token was accepted on `/wallet/balance`, silently treating the
  admin's id as a customer id.
- Admin accounts have no self-registration; they are created only via
  `npm run seed:admin`.
- PINs and admin passwords are argon2 hashed. OTP codes are HMAC-hashed
  with bounded attempts and expiry.

**Auditor should probe:** cross-space token acceptance on every route
added after this document was written. The pattern is easy to get right
and easy to forget.

## 3. Known gaps (not defects — unbuilt)

| Gap | Consequence | Notes |
|---|---|---|
| Admin JWT in `localStorage`, not an httpOnly cookie | XSS in the back-office would expose an operator token | Documented in `lib/api.ts`. Acceptable only while the back-office stays on a trusted internal network |
| No request-rate limiting anywhere | PIN brute-force is bounded per account (5 failed attempts → 15-minute lock, `registerFailedPinAttempt`) but nothing bounds request *rate*: an attacker can spread guesses across many accounts, and each attempt still costs a full argon2 verify before the lock is checked | Redis is already a dependency, so a throttler has somewhere to live |
| No MonCash balance API integration | Reconciliation compares internal ledger against internal float only — it cannot detect a divergence from MonCash's actual balance | Stated in the API response's own `note` field and surfaced in the back-office UI |
| No offline write queue | A transfer attempted with no coverage is not replayed | Every write already carries a client idempotency key, so this can be added safely later |
| SAR filing is documentation only | Nothing is transmitted to UCREF | Deliberate — see `compliance` module |
| Offset-based pagination | Deep offsets degrade on large tables | Fine at current volumes; keyset pagination when `ledger_entries` grows |
| No secrets scanning in CI | A committed secret would not be caught automatically | Environments never share credentials (NF-24), and `.env` files are gitignored |
| KYC document URLs are stored as plain strings | Whatever storage they point at is outside this repo's threat model | The storage bucket's access control is a deployment concern, not an application one |

## 4. What CI actually guarantees

`.github/workflows/ci.yml` runs on every PR against throwaway
`lajanm_ci` credentials that are structurally distinct from dev, staging
and production (NF-24):

1. `lint` across all three packages — includes the mobile i18n check, which
   fails the build on a missing translation key or locale drift.
2. `test` — unit tests with in-memory doubles.
3. `test:integration` — the money invariants above, against a real,
   migrated Postgres. This tier exists because a raw-SQL column-casing bug
   shipped through three modules invisibly: the in-memory double never
   executes SQL.
4. `check:migrations-reversible` — walks every migration down and back up,
   so a broken `down()` fails a PR instead of a production rollback.
5. `build`.

CI sets no `MONCASH_*` credentials, so the MonCash client is inert there
and no test can reach an external payment rail.

## 5. Suggested audit scope, in priority order

1. The ledger module, and specifically whether any code path outside
   `LedgerService.postOperation` can write to `ledger_entries`.
2. The two guards, on every route.
3. The MonCash webhook signature verification (`rawBody` handling in
   `main.ts` exists precisely so the HMAC is computed over the exact bytes
   received, not a re-serialised copy).
4. The payout reserve/confirm/reverse path, including what happens when the
   rail times out after a reservation.
5. CORS configuration before deployment — `CORS_ORIGINS` defaults to
   reflecting the request origin when unset, which is right for local dev
   and **must** be an explicit allowlist in staging and production.
