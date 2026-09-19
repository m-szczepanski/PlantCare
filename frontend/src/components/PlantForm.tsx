import { useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ProfileCombobox } from "@/components/ProfileCombobox";
import { usePlantProfiles } from "@/hooks/usePlantProfiles";
import { touchButton, touchField } from "@/lib/ui";
import type { Plant, PlantInput } from "@/api/types";

function toDateValue(iso: string | null | undefined): string {
  return iso ? iso.slice(0, 10) : "";
}

function fromDateString(value: string): string | null {
  return value ? `${value}T00:00:00` : null;
}

export interface PlantFormProps {
  initial?: Plant;
  submitting: boolean;
  error?: string | null;
  fieldErrors?: Record<string, string>;
  submitLabel: string;
  onSubmit: (input: PlantInput) => void;
  onCancel: () => void;
}

export function PlantForm({ initial, submitting, error, fieldErrors = {}, submitLabel, onSubmit, onCancel }: PlantFormProps) {
  const { data: profiles = [] } = usePlantProfiles();

  const [nickName, setNickName] = useState(initial?.nickName ?? "");
  const [location, setLocation] = useState(initial?.location ?? "");
  const [photoUrl, setPhotoUrl] = useState(initial?.photoUrl ?? "");
  const [profileId, setProfileId] = useState<number | null>(initial?.plantProfileId ?? null);
  const [customInterval, setCustomInterval] = useState<string>(
    initial?.customWateringIntervalDays?.toString() ?? "",
  );
  const [acquiredDate, setAcquiredDate] = useState<string>(toDateValue(initial?.acquiredDate) || toDateValue(new Date().toISOString()));
  const [lastWateredAt, setLastWateredAt] = useState<string>(toDateValue(initial?.lastWateredAt));

  const selectedProfile = profiles.find((profile) => profile.id === profileId);
  const customDays = customInterval.trim() === "" ? null : Number(customInterval);
  const effectiveInterval =
    customDays !== null && Number.isFinite(customDays)
      ? customDays
      : selectedProfile?.defaultWateringIntervalDays ?? null;

  const nextDuePreview = (() => {
    if (effectiveInterval === null || !Number.isInteger(effectiveInterval) || effectiveInterval < 1) {
      return "No watering schedule — pick a profile or set an interval.";
    }
    const base = lastWateredAt ? new Date(`${lastWateredAt}T00:00:00`) : new Date();
    const due = new Date(base.getTime() + effectiveInterval * 86_400_000);
    const dueMidnight = new Date(due.getFullYear(), due.getMonth(), due.getDate());
    const daysFromToday = Math.round(
      (dueMidnight.getTime() - new Date().setHours(0, 0, 0, 0)) / 86_400_000,
    );
    return `Next watering due: ${due.toLocaleDateString()} (${daysFromToday === 0 ? "today" : `in ${daysFromToday} days`})`;
  })();

  function handleSubmit(event: FormEvent) {
    event.preventDefault();

    const trimmedInterval = customInterval.trim();
    const intervalDays = trimmedInterval === "" ? null : Number(trimmedInterval);

    onSubmit({
      nickName: nickName.trim(),
      location: location.trim(),
      photoUrl: photoUrl.trim() || null,
      acquiredDate: fromDateString(acquiredDate) ?? new Date().toISOString(),
      plantProfileId: profileId,
      customWateringIntervalDays: intervalDays !== null && Number.isFinite(intervalDays) ? intervalDays : null,
      lastWateredAt: fromDateString(lastWateredAt),
    });
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="nickName">Nick name</Label>
        <Input
          id="nickName"
          required
          value={nickName}
          onChange={(e) => setNickName(e.target.value)}
          placeholder="Monstera Mike"
          aria-invalid={fieldErrors.nickName ? true : undefined}
          className={touchField}
        />
        <FieldError message={fieldErrors.nickName} />
      </div>

      <div className="space-y-2">
        <Label htmlFor="location">Location</Label>
        <Input
          id="location"
          required
          value={location}
          onChange={(e) => setLocation(e.target.value)}
          placeholder="Living room window"
          aria-invalid={fieldErrors.location ? true : undefined}
          className={touchField}
        />
        <FieldError message={fieldErrors.location} />
      </div>

      <div className="space-y-2">
        <Label htmlFor="profile">Species profile</Label>
        <ProfileCombobox id="profile" profiles={profiles} value={profileId} onChange={setProfileId} />
        <FieldError message={fieldErrors.plantProfileId} />
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor="customInterval">Custom interval (days)</Label>
          <Input
            id="customInterval"
            type="number"
            min={1}
            value={customInterval}
            onChange={(e) => setCustomInterval(e.target.value)}
            placeholder={selectedProfile ? `Profile default: ${selectedProfile.defaultWateringIntervalDays}` : "Overrides profile"}
            aria-invalid={fieldErrors.customWateringIntervalDays ? true : undefined}
            className={touchField}
          />
          <FieldError message={fieldErrors.customWateringIntervalDays} />
        </div>

        <div className="space-y-2">
          <Label htmlFor="acquiredDate">Acquired date</Label>
          <Input
            id="acquiredDate"
            type="date"
            required
            value={acquiredDate}
            onChange={(e) => setAcquiredDate(e.target.value)}
            aria-invalid={fieldErrors.acquiredDate ? true : undefined}
            className={touchField}
          />
          <FieldError message={fieldErrors.acquiredDate} />
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor="lastWateredAt">Last watered</Label>
          <Input
            id="lastWateredAt"
            type="date"
            value={lastWateredAt}
            onChange={(e) => setLastWateredAt(e.target.value)}
            aria-invalid={fieldErrors.lastWateredAt ? true : undefined}
            className={touchField}
          />
          <FieldError message={fieldErrors.lastWateredAt} />
        </div>

        <div className="space-y-2">
          <Label htmlFor="photoUrl">Photo URL</Label>
          <Input
            id="photoUrl"
            value={photoUrl}
            onChange={(e) => setPhotoUrl(e.target.value)}
            placeholder="https://..."
            aria-invalid={fieldErrors.photoUrl ? true : undefined}
            className={touchField}
          />
          <FieldError message={fieldErrors.photoUrl} />
        </div>
      </div>

      <p
        className="rounded-md bg-muted px-3 py-2 text-sm text-muted-foreground"
        role="status"
      >
        {nextDuePreview}
      </p>

      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      <div className="flex flex-wrap gap-2 pt-2">
        <Button type="submit" disabled={submitting} className={touchButton}>
          {submitting ? "Saving..." : submitLabel}
        </Button>
        <Button type="button" variant="ghost" onClick={onCancel} disabled={submitting} className={touchButton}>
          Cancel
        </Button>
      </div>
    </form>
  );
}

function FieldError({ message }: { message?: string }) {
  if (!message) return null;
  return <p className="text-sm text-destructive">{message}</p>;
}
