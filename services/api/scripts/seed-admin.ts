/**
 * Creates (or promotes) an admin_users row. There is no public
 * registration endpoint for admin accounts by design — see AdminUser
 * entity doc — so this script is the only way to create the first admin.
 *
 * Usage:
 *   npm run seed:admin -- --email=ops@lajanm.example --password=... --role=admin
 *
 * Requires the same DATABASE_URL the API itself uses (loaded via dotenv,
 * same as src/database/data-source.ts).
 */
import 'reflect-metadata';
import * as argon2 from 'argon2';
import * as dotenv from 'dotenv';
import { DataSource } from 'typeorm';
import { AdminRole, AdminUser } from '../src/modules/admin/entities/admin-user.entity';

dotenv.config();

function parseArgs(): { email: string; password: string; role: AdminRole; fullName?: string } {
  const args = Object.fromEntries(
    process.argv.slice(2).map((arg) => {
      const [key, ...rest] = arg.replace(/^--/, '').split('=');
      return [key, rest.join('=')];
    }),
  );

  if (!args.email || !args.password) {
    throw new Error('Usage: seed-admin --email=<email> --password=<password> [--role=admin|operator] [--fullName=<name>]');
  }
  const role = (args.role as AdminRole) ?? AdminRole.OPERATOR;
  if (!Object.values(AdminRole).includes(role)) {
    throw new Error(`Invalid role "${role}" — must be one of: ${Object.values(AdminRole).join(', ')}`);
  }
  if (args.password.length < 8) {
    throw new Error('Password must be at least 8 characters');
  }

  return { email: args.email, password: args.password, role, fullName: args.fullName };
}

async function main() {
  const { email, password, role, fullName } = parseArgs();

  const dataSource = new DataSource({
    type: 'postgres',
    url: process.env.DATABASE_URL,
    entities: [__dirname + '/../src/**/*.entity{.ts,.js}'],
  });
  await dataSource.initialize();

  const repo = dataSource.getRepository(AdminUser);
  const passwordHash = await argon2.hash(password);
  const existing = await repo.findOneBy({ email });

  if (existing) {
    existing.passwordHash = passwordHash;
    existing.role = role;
    if (fullName) existing.fullName = fullName;
    await repo.save(existing);
    console.log(`Updated existing admin ${email} (role: ${role})`);
  } else {
    await repo.save(repo.create({ email, passwordHash, role, fullName: fullName ?? null }));
    console.log(`Created admin ${email} (role: ${role})`);
  }

  await dataSource.destroy();
}

main().catch((error) => {
  console.error(error.message ?? error);
  process.exit(1);
});
