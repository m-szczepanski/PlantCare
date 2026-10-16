import i18n from "@/i18n";

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
  if (days === null) return i18n.t("dates.notWatered");
  if (days <= 0) return i18n.t("dates.wateredToday");
  if (days === 1) return i18n.t("dates.wateredYesterday");
  if (days >= 14) return i18n.t("dates.wateredWeeksAgo", { count: Math.floor(days / 7) });
  return i18n.t("dates.wateredDaysAgo", { count: days });
}

// Calendar-day values (DateOnly on the server) carry no timezone; read the
// yyyy-mm-dd prefix as a local day so it never shifts across time zones.
export function parseDateOnly(value: string): Date {
  const [year, month, day] = value.slice(0, 10).split("-").map(Number);
  return new Date(year, month - 1, day);
}

export function dateKey(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}

export function formatDate(value: string | null | undefined, locale: string): string {
  if (!value) return "-";
  return parseDateOnly(value).toLocaleDateString(locale);
}

export function formatInstant(value: string | null | undefined, withTime = true): string {
  if (!value) return "-";
  const date = parseInstant(value);
  const locale = i18n.language;
  return withTime ? date.toLocaleString(locale) : date.toLocaleDateString(locale);
}
