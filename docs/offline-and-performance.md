# Offline behavior and performance

The product targets users on 2G/EDGE connections with intermittent
coverage. That constraint drives three rules that the code follows
throughout, and which any new screen or endpoint is expected to keep.

## 1. Never claim an outcome the server did not confirm

The most damaging failure in a wallet is not an error message — it is a
confident lie. A request that gets **no answer** (timeout, no coverage) is
genuinely ambiguous: it may have reached the server and succeeded.

`apiRequest` (`apps/mobile/src/api/client.ts`) makes that distinction
explicit in the type system:

| Thrown | Meaning | What the UI may say |
|---|---|---|
| `ApiError` | The server answered and rejected the request | The specific outcome ("insufficient funds") |
| `NetworkError` | No answer — timed out, or offline | Only "not confirmed, check your history" |

Screens that move money branch on this, and must keep doing so:

- `TransferScreen` → `offline` state: "not yet confirmed, try again".
- `PayoutScreen` → `unconfirmed` state.

`PayoutScreen`'s failure copy is why this matters. Its `failed` state says
*"the money was returned to your account"* — true when the server rejected
the payout (the reserve is reversed, see the reserve/confirm/reverse
pattern in `docs/architecture.md`), and a dangerous falsehood if shown for
a timeout, where the payout may be in flight. The two states exist
precisely so that sentence is only ever shown when it is true.

## 2. Every request is bounded in time

`REQUEST_TIMEOUT_MS` (20s) caps every mobile API call via `AbortController`.
Without it, `fetch` on a stalled 2G connection waits effectively forever and
the user sits on a spinner unable to tell slow from dead. 20s is generous
for a working-but-slow connection while still failing within a period
someone will wait through.

A non-JSON response (a captive portal or gateway returning HTML) surfaces
as an `ApiError` carrying the HTTP status, not a `JSON.parse` crash — a
parse crash would otherwise be misread by screens as a connectivity
problem.

## 3. Every list endpoint is bounded in size

An unbounded list is both a real monetary cost to a user paying per
kilobyte and an operational cliff for the back-office as data grows.

`services/api/src/common/dto/pagination-query.dto.ts` defines the shared
contract, matching the convention wallet history established:

- `limit` — defaults to **20**, hard-capped at **100**
- `offset` — defaults to 0

`toFindPaging()` clamps in code as well as at the DTO, so no internal
caller can bypass the cap. Every list endpoint accepts these:

| Endpoint | Notes |
|---|---|
| `GET /auth/devices` | |
| `GET /kyc/submissions/me`, `GET /kyc/submissions/queue` | |
| `GET /topup/history`, `GET /payout/history` | |
| `GET /wallet/history` | Also filters by type/date range |
| `GET /fraud/flags` | |
| `GET /compliance/disputes`, `/disputes/me`, `/sar` | |
| `GET /support/tickets/me`, `/support/tickets/queue` | Queue also filters by status |
| `GET /support/faq`, `/support/faq/all` | |

### The one deliberate exception: ticket threads

`GET /support/tickets/:id` returns the whole conversation, capped at
`MAX_THREAD_MESSAGES` (500) rather than paged. A support thread read
oldest-first would, if truncated by a normal `take`, drop the *newest*
messages — the part of the conversation nobody can afford to lose. The
query therefore reads newest-first and flips the result back into reading
order, so the cap can only ever discard the oldest messages of a
pathological thread. No real thread approaches 500.

## Caching

`useBalance` shows the last known balance from `AsyncStorage` immediately
so the home screen is never blank, but any render backed by cache exposes
`isFromCache` and `asOf`, and `HomeScreen` shows the staleness timestamp.
Displaying cached data is fine; silently passing it off as live is not.

## What is not done yet

- No offline write queue. A transfer attempted with no coverage is not
  stored for later replay — the user is told it was not confirmed and
  retries manually. Every write already carries a client-generated
  idempotency key, so a replay queue can be added without risking double
  spends, but it is not built.
- No offline history/transaction cache (only the balance is cached). The
  encrypted-local-storage row in `docs/architecture.md` covers this.
- Pagination is offset-based. Fine at current volumes; deep offsets on a
  large `ledger_entries` will eventually want keyset pagination.
