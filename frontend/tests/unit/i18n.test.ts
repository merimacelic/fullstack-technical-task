import { describe, expect, it } from 'vitest';
import en from '@/i18n/locales/en.json';
import mt from '@/i18n/locales/mt.json';

// Parity guard: every key present in one locale file must exist in the other.
// Catches a new string being added without its Maltese counterpart before the
// reviewer spots a raw translation key in the UI.

type Json = Record<string, unknown>;

function flatten(obj: Json, prefix = ''): [string, string][] {
  return Object.entries(obj).flatMap(([k, v]) => {
    const key = prefix ? `${prefix}.${k}` : k;
    return v && typeof v === 'object' && !Array.isArray(v)
      ? flatten(v as Json, key)
      : [[key, String(v)] as [string, string]];
  });
}

// Keys whose EN and MT values are *intentionally* identical — brand names,
// technical labels that stay in English, or proper nouns. Listed here so the
// "untranslated" check below doesn't flag them as drift.
const IDENTICAL_VALUE_ALLOWLIST = new Set<string>([
  'errors.notFoundPage.code', // "404"
  'header.language.en', // "English" (label of the EN option in the menu)
  'header.language.mt', // "Malti" (self-name — same in both files)
  'tasks.filters.placeholders.status', // "Status"
  'tasks.filters.placeholders.tag', // "Tag"
  'tasks.form.fields.status', // "Status"
  'tasks.form.fields.tags', // "Tags"
  'tasks.page.tagsButton', // "Tags"
  'tasks.table.headers.status', // "Status"
  'tasks.table.headers.tags', // "Tags"
  'tasks.status.ariaLabel', // "Status: {{label}}" — "Status" is borrowed verbatim in MT
  'tags.manager.title', // "Tags"
  'auth.fields.email', // "Email"
  'auth.fields.password', // "Password"
]);

describe('i18n key parity', () => {
  const enEntries = flatten(en as Json);
  const mtEntries = flatten(mt as Json);
  const enKeys = new Set(enEntries.map(([k]) => k));
  const mtKeys = new Set(mtEntries.map(([k]) => k));
  const enByKey = new Map(enEntries);
  const mtByKey = new Map(mtEntries);

  it('every English key has a Maltese counterpart', () => {
    const missing = [...enKeys].filter((k) => !mtKeys.has(k));
    expect(missing).toEqual([]);
  });

  it('every Maltese key has an English counterpart', () => {
    const extra = [...mtKeys].filter((k) => !enKeys.has(k));
    expect(extra).toEqual([]);
  });

  // Soft check — catches copy-pasted placeholders where a Maltese entry was
  // left equal to its English source during translation. The allowlist above
  // carries the small set of keys that really should match in both files.
  it('Maltese values differ from English unless explicitly allowed', () => {
    const untranslated: string[] = [];
    for (const [key, enValue] of enByKey) {
      if (IDENTICAL_VALUE_ALLOWLIST.has(key)) continue;
      const mtValue = mtByKey.get(key);
      if (mtValue !== undefined && mtValue === enValue) {
        untranslated.push(key);
      }
    }
    expect(untranslated).toEqual([]);
  });
});
