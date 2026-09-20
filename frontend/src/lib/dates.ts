const DAY_MS = 86_400_000;

// SQLite answers with naive UTC strings (no suffix); Postgres returns proper
// ISO offsets. Append Z only when the value carries no timezone designator.
export function hasTimezoneDesignator(value: string): boolean {
  return /[zZ]$|[+-]\d{2}:?\d{2}$/.test(value);
}

export function parseInstant(value: string): Date {
  return new Date(hasTimezoneDesignator(value) ? value : `${value}Z`);
}

export function daysSince(value: string | null): number | null {
  if (!value) return null;
  return Math.floor((Date.now() - parseInstant(value).getTime()) / DAY_MS);
}

export function wateredRelative(value: string | null): string {
  const days = daysSince(value);
  if (days === null) return "Not watered yet";
  if (days <= 0) return "Watered today";
  if (days === 1) return "Watered yesterday";
  if (days >= 14) return `Watered ${Math.floor(days / 7)} weeks ago`;
  return `Watered ${days} days ago`;
}

export function formatInstant(value: string | null | undefined, withTime = true): string {
  if (!value) return "-";
  const date = parseInstant(value);
  return withTime ? date.toLocaleString() : date.toLocaleDateString();
}
