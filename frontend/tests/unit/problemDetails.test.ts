import { describe, expect, it } from 'vitest';
import { parseProblem } from '@/shared/lib/problemDetails';

describe('parseProblem', () => {
  it('returns network-error fallback for FETCH_ERROR', () => {
    const result = parseProblem({ status: 'FETCH_ERROR', error: 'boom' } as never);
    expect(result.title).toBe('Network error');
  });

  it('extracts ProblemDetails fields from a 404 response', () => {
    const result = parseProblem({
      status: 404,
      data: {
        type: 'Task.NotFound',
        title: 'Not found',
        status: 404,
        detail: 'Task not found.',
      },
    } as never);
    expect(result.status).toBe(404);
    expect(result.title).toBe('Not found');
    expect(result.detail).toBe('Task not found.');
    expect(result.type).toBe('Task.NotFound');
  });

  it('surfaces field errors from a 400 validation response', () => {
    const result = parseProblem({
      status: 400,
      data: {
        title: 'Validation',
        status: 400,
        errors: { Title: ['Title is required.'] },
      },
    } as never);
    expect(result.fieldErrors).toEqual({ Title: ['Title is required.'] });
  });

  it('falls back for 429', () => {
    const result = parseProblem({ status: 429, data: {} } as never);
    expect(result.title).toBe('Too many requests');
  });
});
