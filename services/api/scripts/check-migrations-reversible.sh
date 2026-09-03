#!/usr/bin/env bash
#
# Proves every migration can be rolled back and re-applied.
#
# A broken down() migration is normally discovered at the worst possible
# moment — during an incident rollback in production. This walks the whole
# stack down and back up against a real database so that failure surfaces
# in CI instead.
#
# Requires DATABASE_URL pointing at a database with migrations applied.
set -euo pipefail

cd "$(dirname "$0")/.."

count_applied() {
  npm run --silent typeorm -- migration:show -d src/database/data-source.ts 2>/dev/null \
    | grep -c '\[X\]' || true
}

applied_before=$(count_applied)
echo "migrations applied: ${applied_before}"

if [ "${applied_before}" -eq 0 ]; then
  echo "No migrations applied — run 'npm run migration:run' first." >&2
  exit 1
fi

echo "--- reverting all ---"
for _ in $(seq 1 "${applied_before}"); do
  npm run --silent migration:revert > /dev/null
done

remaining=$(count_applied)
if [ "${remaining}" -ne 0 ]; then
  echo "FAIL: ${remaining} migration(s) still applied after reverting all of them." >&2
  exit 1
fi
echo "all ${applied_before} migrations reverted cleanly"

echo "--- re-applying all ---"
npm run --silent migration:run > /dev/null

applied_after=$(count_applied)
if [ "${applied_after}" -ne "${applied_before}" ]; then
  echo "FAIL: re-applied ${applied_after}, expected ${applied_before}." >&2
  exit 1
fi

echo "OK: ${applied_after} migrations survive a full down/up cycle"
