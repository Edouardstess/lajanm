import 'reflect-metadata';
import { DataSource } from 'typeorm';
import * as dotenv from 'dotenv';

dotenv.config();

/**
 * Standalone TypeORM DataSource used only by the CLI (migration:generate/run/revert).
 * The running application builds its own connection via TypeOrmModule.forRootAsync
 * (see src/database/database.module.ts) so that it can pull config from ConfigService
 * instead of process.env directly.
 */
export const AppDataSource = new DataSource({
  type: 'postgres',
  url: process.env.DATABASE_URL,
  entities: [__dirname + '/../**/*.entity{.ts,.js}'],
  migrations: [__dirname + '/migrations/*{.ts,.js}'],
  synchronize: false,
});
