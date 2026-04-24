// Matches the backend CorrelationId middleware format: 32 hex chars, no
// hyphens. Echoed back by the API in the X-Correlation-Id response header so
// support + logs can tie a client action to server-side traces.

export function newCorrelationId(): string {
  const uuid = typeof crypto !== 'undefined' && 'randomUUID' in crypto
    ? crypto.randomUUID()
    : fallbackUuid();
  return uuid.replaceAll('-', '');
}

function fallbackUuid(): string {
  // RFC 4122 v4 with Math.random — only hit in test/jsdom contexts where
  // crypto.randomUUID isn't exposed.
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = Math.floor(Math.random() * 16);
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}
