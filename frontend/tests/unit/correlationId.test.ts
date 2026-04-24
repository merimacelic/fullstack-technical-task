import { describe, expect, it } from 'vitest';
import { newCorrelationId } from '@/shared/lib/correlationId';

describe('newCorrelationId', () => {
  it('produces a 32-char lowercase hex string', () => {
    const id = newCorrelationId();
    expect(id).toHaveLength(32);
    expect(id).toMatch(/^[0-9a-f]{32}$/);
    expect(id).not.toContain('-');
  });

  it('produces unique values across calls', () => {
    const ids = new Set(Array.from({ length: 64 }, () => newCorrelationId()));
    expect(ids.size).toBe(64);
  });
});
