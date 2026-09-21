import type { SoilType, SoilTypeOption } from "@/api/types";

export function soilFactor(options: SoilTypeOption[], type: SoilType | null): number {
  if (!type) return 1;
  return options.find((option) => option.type === type)?.wateringIntervalFactor ?? 1;
}

export function applySoilFactor(
  options: SoilTypeOption[],
  type: SoilType | null,
  baseIntervalDays: number | null,
): number | null {
  if (baseIntervalDays === null || !Number.isInteger(baseIntervalDays) || baseIntervalDays < 1) {
    return null;
  }
  // Positive values only, so Math.round matches the backend's away-from-zero rounding.
  return Math.max(1, Math.round(baseIntervalDays * soilFactor(options, type)));
}
