import { useTranslation } from "react-i18next";
import { Globe } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { setLanguage, SUPPORTED_LANGUAGES, LANGUAGE_LABELS, activeLanguage } from "@/i18n";

export function LanguageToggle() {
  const { t } = useTranslation();
  const current = activeLanguage();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" aria-label={t("language.toggle")} className="h-11 w-11 sm:h-9 sm:w-9">
          <Globe className="h-[1.2rem] w-[1.2rem]" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {SUPPORTED_LANGUAGES.map((language) => (
          <DropdownMenuItem
            key={language}
            aria-current={language === current ? "true" : undefined}
            onSelect={() => {
              if (language !== current) {
                void setLanguage(language);
              }
            }}
          >
            {LANGUAGE_LABELS[language]}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
