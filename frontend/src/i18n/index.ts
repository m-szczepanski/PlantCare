import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import en from "./locales/en.json";
import pl from "./locales/pl.json";

export const LANGUAGE_STORAGE_KEY = "ui-language";
export const SUPPORTED_LANGUAGES = ["en", "pl"] as const;
export type Language = (typeof SUPPORTED_LANGUAGES)[number];

export const LANGUAGE_LABELS: Record<Language, string> = {
  en: "English",
  pl: "Polski",
};

export function isSupportedLanguage(value: string | null | undefined): value is Language {
  return !!value && (SUPPORTED_LANGUAGES as readonly string[]).includes(value);
}

function readStoredLanguage(): string | null {
  try {
    return window.localStorage.getItem(LANGUAGE_STORAGE_KEY);
  } catch {
    return null;
  }
}

function detectInitialLanguage(): Language {
  const stored = readStoredLanguage();
  if (isSupportedLanguage(stored)) {
    return stored;
  }
  return (navigator.language ?? "").toLowerCase().startsWith("pl") ? "pl" : "en";
}

void i18n.use(initReactI18next).init({
  resources: { en: { translation: en }, pl: { translation: pl } },
  lng: detectInitialLanguage(),
  fallbackLng: "en",
  interpolation: { escapeValue: false },
});

function applyDocumentLanguage(language: string) {
  document.documentElement.lang = language;
}

applyDocumentLanguage(i18n.language);
i18n.on("languageChanged", applyDocumentLanguage);

export function setLanguage(language: Language) {
  try {
    window.localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
  } catch {
    // storage unavailable (private mode / test stub not installed yet)
  }
  return i18n.changeLanguage(language);
}

export function activeLanguage(): Language {
  return isSupportedLanguage(i18n.language) ? i18n.language : "en";
}

export default i18n;
