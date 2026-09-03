# Lajan'm

Lajan'm is a mobile wallet for Haiti (iOS + Android + web back-office). This
repository is a monorepo containing the mobile app, the admin back-office,
and the core API.

> **Governance note.** Lajan'm holding a client balance on its own ledger
> puts it close to the Fournisseur de Services de Paiement (FSP) status
> defined by BRH Circular n°121. Technical development proceeds in
> parallel with legal clarification, but **no production launch with real
> user funds should happen before that status is clarified** — keep the
> app in sandbox/staging with test accounts until then. See
> `docs/architecture.md` for how MVP-critical compliance requirements
> (100,000 HTG/transaction cap, fund segregation, data retention, UCREF
> reporting hooks) are built in from the start rather than retrofitted.

## Repository layout

```
lajanm/
├── apps/
│   ├── mobile/          # React Native (Expo) — client app
│   └── admin-web/       # Next.js — back-office
├── services/
│   └── api/             # NestJS — core API (auth, KYC, ledger, payments, ...)
├── infra/                # Docker, CI/CD, deployment config
└── docs/                 # architecture, API, runbooks
```

## Prerequisites

- Node.js 20+
- Docker (for local Postgres/Redis via `docker-compose.yml`)

## Local development

```bash
npm install

# Start Postgres + Redis
docker compose up -d postgres redis

# Configure the API
cp services/api/.env.development.example services/api/.env

# Apply database migrations
npm run migration:run -w @lajanm/api

# Start the API in watch mode
npm run dev -w @lajanm/api
```

The API listens on `http://localhost:3000` (`GET /health` for a liveness
check). Full stack, including the containerized API, can also be brought up
with `docker compose up`.

### Common commands (run from the repo root, via Turborepo)

| Command | What it does |
|---|---|
| `npm run build` | Build every app/service |
| `npm run lint` | Lint every app/service |
| `npm test` | Run every test suite |
| `npm run dev` | Run all apps/services in watch mode |

### API-specific commands (`-w @lajanm/api`)

| Command | What it does |
|---|---|
| `npm run migration:generate -w @lajanm/api -- <Name>` | Generate a migration from entity changes |
| `npm run migration:run -w @lajanm/api` | Apply pending migrations (development — uses ts-node) |
| `npm run migration:run:prod -w @lajanm/api` | Apply pending migrations from a production image (compiled data-source, no ts-node) |
| `npm run migration:revert -w @lajanm/api` | Revert the last migration |
| `npm run test:integration -w @lajanm/api` | Run the money-invariant tests against a real, migrated Postgres |
| `npm run check:migrations-reversible -w @lajanm/api` | Walk every migration down and back up |
| `npm run seed:admin -w @lajanm/api -- --email=... --password=... --role=admin` | Create a back-office account (there is no self-registration) |

## Environments

Development, staging, and production are fully separate: distinct
databases, distinct Redis instances, distinct secrets. Nothing is shared
between them, and the API refuses to boot if required env vars are missing
(see `services/api/src/config/env.validation.ts`). See
`services/api/.env.*.example` for the variables each environment needs.

## The ledger

The financial core of the system is a double-entry ledger
(`services/api/src/modules/ledger`): every movement of money is a balanced
set of debit/credit entries tied to an idempotency key, and ledger entries
are never updated or deleted once written — corrections are posted as new,
reversing entries. See `docs/architecture.md` for the full design
rationale and `LedgerService`'s tests for the invariants this guarantees.

## Documentation

| Document | Contents |
|---|---|
| `docs/architecture.md` | Stack choices, the ledger design, regulatory-driven technical requirements |
| `docs/topup.md` | The MonCash top-up flow and its webhook |
| `docs/offline-and-performance.md` | Behaviour on 2G/EDGE: what the app may claim when a request gets no answer, request timeouts, and payload bounds |
| `docs/audit-readiness.md` | What an external security auditor should look at, what is in place, and the known gaps — read this before commissioning an audit |
| `docs/release.md` | Deploying the API and building the Android/iOS installers: what you must supply (accounts, domain), and the exact commands |
