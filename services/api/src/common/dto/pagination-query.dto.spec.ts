import { DEFAULT_PAGE_SIZE, MAX_PAGE_SIZE, toFindPaging } from './pagination-query.dto';

describe('toFindPaging', () => {
  it('applies the default page size when the caller asks for nothing', () => {
    expect(toFindPaging()).toEqual({ take: DEFAULT_PAGE_SIZE, skip: 0 });
    expect(toFindPaging({})).toEqual({ take: DEFAULT_PAGE_SIZE, skip: 0 });
  });

  it('honours an explicit limit and offset', () => {
    expect(toFindPaging({ limit: 5, offset: 10 })).toEqual({ take: 5, skip: 10 });
  });

  // The DTO's @Max already rejects an oversized limit at the HTTP edge, but
  // internal callers pass options straight through — the clamp here is what
  // guarantees no code path can ask the database for an unbounded page.
  it('clamps a limit above the maximum', () => {
    expect(toFindPaging({ limit: 5000 }).take).toBe(MAX_PAGE_SIZE);
  });

  it('treats a missing offset as the first page', () => {
    expect(toFindPaging({ limit: 50 })).toEqual({ take: 50, skip: 0 });
  });
});
