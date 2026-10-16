import i18n from "@/i18n";

export const API_BASE_URL = "/api";

export function apiHeaders(): Record<string, string> {
  return { "Accept-Language": i18n.language };
}
