// Thin wrapper over localStorage scoped to the refresh token. Access tokens
// never touch storage — they live in the Redux auth slice and die on reload
// (a fresh access token is minted via the refresh token on bootstrap).
//
// Security note: a leaked refresh token is one-shot. The backend rotates every
// use and revokes the whole token family on replay (see ADR 0008 for the
// trade-off and the future HttpOnly-cookie migration path).

// Exported so cross-tab listeners can filter storage events by key without
// duplicating the string. Anything touching this key owes the auth slice a
// consistent dispatch — don't bypass tokenStorage.
export const REFRESH_TOKEN_KEY = 'taskmanagement.refresh_token';

function safeLocalStorage(): Storage | null {
  try {
    return typeof window !== 'undefined' ? window.localStorage : null;
  } catch {
    // e.g. privacy mode in Safari where localStorage access throws.
    return null;
  }
}

export const tokenStorage = {
  getRefreshToken(): string | null {
    return safeLocalStorage()?.getItem(REFRESH_TOKEN_KEY) ?? null;
  },
  setRefreshToken(token: string): void {
    safeLocalStorage()?.setItem(REFRESH_TOKEN_KEY, token);
  },
  clearRefreshToken(): void {
    safeLocalStorage()?.removeItem(REFRESH_TOKEN_KEY);
  },
};
