import i18n from "@/i18n";

export const API_BASE_URL = "/api";

export function apiHeaders(extra?: HeadersInit): Record<string, string> {
  return { "Accept-Language": i18n.language, ...(extra as Record<string, string>) };
}
