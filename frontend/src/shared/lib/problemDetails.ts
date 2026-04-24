// RFC 7807 ProblemDetails parser. The backend emits this shape for every
// failure (see TaskManagement.Api/Infrastructure/ErrorOrResults.cs). Validation
// failures come with an `errors` bag keyed by field name; other failures carry
// a dotted Type (e.g. "Task.NotFound") and a human-readable Detail.
//
// This module is intentionally framework-free — it returns i18n KEYS rather
// than resolved strings. Callers (components) translate via useTranslation's
// t(). Keeping t() out of here avoids touching every call site with a new
// argument and preserves this file's "pure parser" contract.

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
  // When the BE sent a localised Title/Detail we prefer those verbatim;
  // otherwise we fall back to an i18n key the caller can translate.
  title: string;
  detail: string;
  titleKey: string;
  detailKey: string;
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
    return blank('errors.unknown.title', 'errors.unknown.detail', 0);
  }

  // SerializedError — thrown errors from queryFn etc.
  if ('message' in error && !('status' in error)) {
    return {
      status: 0,
      title: error.name ?? '',
      detail: error.message ?? '',
      titleKey: error.name ? '' : 'errors.generic.title',
      detailKey: error.message ? '' : 'errors.generic.detail',
    };
  }

  const fb = error as FetchBaseQueryError;
  const status = typeof fb.status === 'number' ? fb.status : 0;
  const payload = isProblemDetailsPayload(fb.data) ? fb.data : undefined;

  if (fb.status === 'FETCH_ERROR' || fb.status === 'TIMEOUT_ERROR') {
    return blank('errors.network.title', 'errors.network.detail', 0);
  }

  const { titleKey, detailKey } = keysForStatus(status);

  return {
    status,
    title: payload?.title ?? '',
    detail: payload?.detail ?? '',
    titleKey,
    detailKey,
    type: payload?.type,
    fieldErrors: payload?.errors,
    traceId: payload?.traceId,
  };
}

function blank(titleKey: string, detailKey: string, status: number): ParsedProblem {
  return { status, title: '', detail: '', titleKey, detailKey };
}

function keysForStatus(status: number): { titleKey: string; detailKey: string } {
  if (status >= 500) return { titleKey: 'errors.server.title', detailKey: 'errors.server.detail' };
  if (status === 429) return { titleKey: 'errors.rateLimit.title', detailKey: 'errors.rateLimit.detail' };
  if (status === 404) return { titleKey: 'errors.notFound.title', detailKey: 'errors.notFound.detail' };
  if (status === 409) return { titleKey: 'errors.conflict.title', detailKey: 'errors.conflict.detail' };
  if (status === 401) return { titleKey: 'errors.unauthorised.title', detailKey: 'errors.unauthorised.detail' };
  if (status === 403) return { titleKey: 'errors.forbidden.title', detailKey: 'errors.forbidden.detail' };
  if (status === 400) return { titleKey: 'errors.validation.title', detailKey: 'errors.validation.detail' };
  return { titleKey: 'errors.generic.title', detailKey: 'errors.generic.detail' };
}

// Helper for toast callers: prefer the server-provided localised title/detail
// when present, otherwise fall through to the i18n key so the caller can translate.
export function problemTitle(p: ParsedProblem): string {
  return p.title || p.titleKey;
}
export function problemDetail(p: ParsedProblem): string {
  return p.detail || p.detailKey;
}
