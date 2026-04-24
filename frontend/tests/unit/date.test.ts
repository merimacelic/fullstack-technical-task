import { describe, expect, it } from 'vitest';
import { dateInputToIsoUtc, isPastDate, toDateInputValue } from '@/shared/lib/date';

describe('date helpers', () => {
  it('converts an iso string to a date-input value', () => {
    expect(toDateInputValue('2026-04-24T10:00:00Z').length).toBeGreaterThan(0);
  });

  it('turns a date-input string into utc midnight', () => {
    const iso = dateInputToIsoUtc('2026-05-01');
    expect(iso).toBe('2026-05-01T00:00:00.000Z');
  });

  it('returns null for empty input', () => {
    expect(dateInputToIsoUtc('')).toBeNull();
  });

  it('detects past dates', () => {
    expect(isPastDate('1999-01-01')).toBe(true);
    expect(isPastDate('2099-01-01')).toBe(false);
  });
});
