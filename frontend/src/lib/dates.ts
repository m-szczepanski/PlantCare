const DAY_MS = 86_400_000;

// The API stores watering instants as naive UTC strings; without the Z suffix
// browsers would read them back as local time and the day count would drift.
function parseInstant(value: string): Date {
  return new Date(value.endsWith("Z") ? value : `${value}Z`);
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
