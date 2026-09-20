import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Check, ChevronsUpDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";
import { touchField } from "@/lib/ui";
import type { PlantProfileOption } from "@/api/types";

interface ProfileComboboxProps {
  profiles: PlantProfileOption[];
  value: number | null;
  onChange: (value: number | null) => void;
  id?: string;
}

export function ProfileCombobox({ profiles, value, onChange, id }: ProfileComboboxProps) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const selected = profiles.find((profile) => profile.id === value);

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          id={id}
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className={cn("w-full justify-between font-normal", touchField)}
        >
          {selected ? (
            <span>
              {t("profile.selectedWithInterval", { name: selected.commonName, days: selected.defaultWateringIntervalDays })}
            </span>
          ) : (
            <span className="text-muted-foreground">{t("profile.none")}</span>
          )}
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[--radix-popover-trigger-width] p-0" align="start">
        <Command>
          <CommandInput placeholder={t("profile.searchPlaceholder")} />
          <CommandList>
            <CommandEmpty>{t("profile.noFound")}</CommandEmpty>
            <CommandGroup>
              <CommandItem
                value="none"
                onSelect={() => {
                  onChange(null);
                  setOpen(false);
                }}
              >
                <Check className={cn("h-4 w-4", value === null ? "opacity-100" : "opacity-0")} />
                {t("profile.none")}
              </CommandItem>
              {profiles.map((profile) => (
                <CommandItem
                  key={profile.id}
                  value={profile.commonName}
                  onSelect={() => {
                    onChange(profile.id);
                    setOpen(false);
                  }}
                >
                  <Check
                    className={cn("h-4 w-4", value === profile.id ? "opacity-100" : "opacity-0")}
                  />
                  {t("profile.selectedWithInterval", { name: profile.commonName, days: profile.defaultWateringIntervalDays })}
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}
