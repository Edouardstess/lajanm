import { Type } from 'class-transformer';
import { IsInt, IsOptional, Max, Min } from 'class-validator';

/**
 * Shared list-endpoint pagination, matching the convention wallet history
 * established (limit defaults to 20, hard-capped at 100).
 *
 * Every list endpoint is bounded on purpose. The mobile audience is on 2G/
 * EDGE, where an unbounded response is a real cost to the user, and the
 * back-office queues grow with business volume — an operator opening the
 * ticket queue after a year should not pull every ticket ever filed.
 */
export class PaginationQueryDto {
  @IsOptional()
  @Type(() => Number)
  @IsInt()
  @Min(1)
  @Max(100)
  limit?: number;

  @IsOptional()
  @Type(() => Number)
  @IsInt()
  @Min(0)
  offset?: number;
}

export const DEFAULT_PAGE_SIZE = 20;
export const MAX_PAGE_SIZE = 100;

/** Clamps caller-supplied paging into TypeORM's take/skip. */
export function toFindPaging(query: { limit?: number; offset?: number } = {}) {
  return {
    take: Math.min(query.limit ?? DEFAULT_PAGE_SIZE, MAX_PAGE_SIZE),
    skip: query.offset ?? 0,
  };
}
