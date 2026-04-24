import { format, formatDistanceToNow, isBefore, isValid, parseISO, startOfDay } from 'date-fns';
import { enGB, mt } from 'date-fns/locale';
import type { Locale } from 'date-fns';
import i18n from '@/i18n';

// Pick the date-fns locale that matches i18next's current language. Called per
// format, not cached — i18n.language changes when the user toggles locale and
// we need the next render to pick the new one up.
function currentLocale(): Locale {
  return i18n.language?.toLowerCase().startsWith('mt') ? mt : enGB;
}

export function formatDate(value: string | Date | null | undefined, pattern = 'PP'): string {
  if (!value) return '';
  const date = value instanceof Date ? value : parseISO(value);
  return isValid(date) ? format(date, pattern, { locale: currentLocale() }) : '';
}

export function formatDateTime(value: string | Date | null | undefined): string {
  return formatDate(value, 'PPp');
}

export function formatRelative(value: string | Date | null | undefined): string {
  if (!value) return '';
  const date = value instanceof Date ? value : parseISO(value);
  return isValid(date)
    ? formatDistanceToNow(date, { addSuffix: true, locale: currentLocale() })
    : '';
}

// Input value for <input type="date"> — YYYY-MM-DD in the user's local timezone.
export function toDateInputValue(value: string | Date | null | undefined): string {
  if (!value) return '';
  const date = value instanceof Date ? value : parseISO(value);
  return isValid(date) ? format(date, 'yyyy-MM-dd') : '';
}

// Turn a YYYY-MM-DD string into an ISO UTC datetime at midnight.
export function dateInputToIsoUtc(value: string): string | null {
  if (!value) return null;
  const [year, month, day] = value.split('-').map(Number);
  if (!year || !month || !day) return null;
  const utc = new Date(Date.UTC(year, month - 1, day, 0, 0, 0, 0));
  return utc.toISOString();
}

export function isPastDate(value: string | Date): boolean {
  const date = value instanceof Date ? value : parseISO(value);
  return isValid(date) && isBefore(startOfDay(date), startOfDay(new Date()));
}
