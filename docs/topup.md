# Top-up (MonCash)

## Why the idempotency key matters here specifically

MonCash's webhook delivery is documented (and observed in practice across
integrations) to redeliver the same notification more than once — on a
timeout waiting for our 2xx response, on a retry after a transient network
error, etc. If crediting a user's wallet were a plain `UPDATE balance SET
amount = amount + X`, a redelivered webhook would double-credit the user
for a single real-world MonCash payment. That is not a theoretical edge
case; it is the normal, expected behavior of at-least-once webhook
delivery.

`TopupService.handleWebhook` uses MonCash's own transaction reference
(`payload.reference`) as the ledger's `idempotencyKey` when calling
`LedgerService.postOperation`. The ledger enforces uniqueness on that key
at the database level (`operations.idempotencyKey` is `UNIQUE`), so:

- The first webhook delivery for a reference creates the operation and its
  two balanced ledger entries (credit the user's wallet, debit the
  `moncash_float` system account).
- Every subsequent delivery for the same reference hits the unique
  constraint, and `LedgerService.postOperation` returns the original
  result with `idempotent: true` instead of erroring or duplicating
  entries.
- `TopupService.handleWebhook` doesn't need to special-case this — it
  always resaves the transaction's `status`/`operationId`, which is a
  harmless no-op on a redelivery.

This is verified in `topup.service.spec.ts` ("credits the ledger exactly
once even when the webhook is redelivered") and was also verified manually
against a running instance: sending the same signed webhook payload twice
produced exactly one `operations` row and two `ledger_entries` rows, not
two and four.

## Signature verification

The webhook handler never trusts a payload without first verifying an
HMAC-SHA256 signature (`MonCashClient.verifyWebhookSignature`) computed
over the raw request bytes (`main.ts` enables Nest's `rawBody` option so
the exact bytes MonCash sent are available, not a re-serialized copy that
could differ byte-for-byte). An unsigned or incorrectly signed request is
rejected with 401 before any database read, let alone write.

## What's real vs. placeholder in this module

- The double-entry ledger crediting, the idempotency guarantee, and the
  retry-queue fallback for a MonCash outage are fully implemented and
  tested (unit tests + manual verification against a running instance).
- `MonCashClient`'s actual HTTP request shape (`POST {base}/v1/CreatePayment`,
  Basic auth, `{ reference, gatewayUrl }` response) follows this project's
  architecture plan but has **not** been exercised against a real MonCash
  sandbox — there are no MonCash credentials available in this
  environment. Before this goes anywhere near real money, verify the
  request/response contract against MonCash's actual sandbox docs and
  adjust `MonCashClient.createPayment` accordingly.
- Push notification on credit (mentioned in the product plan) isn't wired
  yet — it lands with the wallet module's notification work.
