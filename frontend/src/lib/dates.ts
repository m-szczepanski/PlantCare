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

export function formatInstant(value: string | null | undefined, withTime = true): string {
  if (!value) return "-";
  const date = parseInstant(value);
  const locale = i18n.language;
  return withTime ? date.toLocaleString(locale) : date.toLocaleDateString(locale);
}
