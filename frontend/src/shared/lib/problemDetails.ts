// RFC 7807 ProblemDetails parser. The backend emits this shape for every
// failure (see TaskManagement.Api/Infrastructure/ErrorOrResults.cs). Validation
// failures come with an `errors` bag keyed by field name; other failures carry
// a dotted Type (e.g. "Task.NotFound") and a human-readable Detail.

import type { FetchBaseQueryError } from '@reduxjs/toolkit/query';
import type { SerializedError } from '@reduxjs/toolkit';

export interface ProblemDetailsPayload {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export interface ParsedProblem {
  status: number;
  title: string;
  detail: string;
  type?: string;
  fieldErrors?: Record<string, string[]>;
  traceId?: string;
}

function isProblemDetailsPayload(value: unknown): value is ProblemDetailsPayload {
  return (
    typeof value === 'object' &&
    value !== null &&
    ('title' in value || 'detail' in value || 'errors' in value)
  );
}

export function parseProblem(
  error: FetchBaseQueryError | SerializedError | undefined,
): ParsedProblem {
  if (!error) {
    return { status: 0, title: 'Unknown error', detail: 'An unknown error occurred.' };
  }

  // SerializedError — thrown errors from queryFn etc.
  if ('message' in error && !('status' in error)) {
    return { status: 0, title: error.name ?? 'Error', detail: error.message ?? 'An error occurred.' };
  }

  const fb = error as FetchBaseQueryError;
  const status = typeof fb.status === 'number' ? fb.status : 0;
  const payload = isProblemDetailsPayload(fb.data) ? fb.data : undefined;

  if (fb.status === 'FETCH_ERROR' || fb.status === 'TIMEOUT_ERROR') {
    return {
      status: 0,
      title: 'Network error',
      detail: 'Unable to reach the server. Check your connection and try again.',
    };
  }

  if (status === 429) {
    return {
      status,
      title: 'Too many requests',
      detail: payload?.detail ?? 'Try again in a moment.',
      type: payload?.type,
    };
  }

  return {
    status,
    title: payload?.title ?? defaultTitleForStatus(status),
    detail: payload?.detail ?? defaultDetailForStatus(status),
    type: payload?.type,
    fieldErrors: payload?.errors,
    traceId: payload?.traceId,
  };
}

function defaultTitleForStatus(status: number): string {
  if (status >= 500) return 'Server error';
  if (status === 404) return 'Not found';
  if (status === 409) return 'Conflict';
  if (status === 401) return 'Unauthorised';
  if (status === 403) return 'Forbidden';
  if (status === 400) return 'Validation error';
  return 'Error';
}

function defaultDetailForStatus(status: number): string {
  if (status >= 500) return 'Something went wrong on our end. Please try again.';
  if (status === 404) return 'The resource you requested was not found.';
  if (status === 409) return 'The request conflicts with the current state.';
  if (status === 401) return 'Your session has expired. Please sign in again.';
  if (status === 403) return 'You are not allowed to perform this action.';
  if (status === 400) return 'Please fix the errors below and try again.';
  return 'An error occurred.';
}
