/**
 * Runs *.integration.spec.ts files against a REAL Postgres — the plain
 * `npm test` config never executes real SQL (see ledger.service.spec.ts's
 * in-memory FakeDataSource), which is exactly how a raw-SQL column-casing
 * bug in LedgerService.getBalance shipped unnoticed through Modules 0-2
 * until a manual smoke test caught it. These tests exist so that class of
 * bug fails CI, not a human running curl by hand.
 *
 * Requires DATABASE_URL to point at a database with migrations already
 * applied (`npm run migration:run`) — see .github/workflows/ci.yml, and
 * locally: docker compose up -d postgres && npm run migration:run -w @lajanm/api.
 */
module.exports = {
  moduleFileExtensions: ['js', 'json', 'ts'],
  rootDir: 'src',
  testRegex: '\\.integration\\.spec\\.ts$',
  transform: { '^.+\\.(t|j)s$': 'ts-jest' },
  testEnvironment: 'node',
};
