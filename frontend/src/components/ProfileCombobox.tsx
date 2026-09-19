import { useState } from "react";
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
              {selected.commonName} ({selected.defaultWateringIntervalDays}d)
            </span>
          ) : (
            <span className="text-muted-foreground">No profile</span>
          )}
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[--radix-popover-trigger-width] p-0" align="start">
        <Command>
          <CommandInput placeholder="Search profiles..." />
          <CommandList>
            <CommandEmpty>No profile found.</CommandEmpty>
            <CommandGroup>
              <CommandItem
                value="none"
                onSelect={() => {
                  onChange(null);
                  setOpen(false);
                }}
              >
                <Check className={cn("h-4 w-4", value === null ? "opacity-100" : "opacity-0")} />
                No profile
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
                  {profile.commonName} ({profile.defaultWateringIntervalDays}d)
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}
