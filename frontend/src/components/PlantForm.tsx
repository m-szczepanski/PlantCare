import { useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { usePlantProfiles } from "@/hooks/usePlantProfiles";
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
  submitLabel: string;
  onSubmit: (input: PlantInput) => void;
  onCancel: () => void;
}

export function PlantForm({ initial, submitting, error, submitLabel, onSubmit, onCancel }: PlantFormProps) {
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
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="location">Location</Label>
        <Input
          id="location"
          required
          value={location}
          onChange={(e) => setLocation(e.target.value)}
          placeholder="Living room window"
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="profile">Species profile</Label>
        <Select
          value={profileId == null ? "none" : String(profileId)}
          onValueChange={(value) => setProfileId(value === "none" ? null : Number(value))}
        >
          <SelectTrigger id="profile" className="w-full">
            <SelectValue placeholder="Select a profile" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="none">No profile</SelectItem>
            {profiles.map((profile) => (
              <SelectItem key={profile.id} value={String(profile.id)}>
                {profile.commonName} ({profile.defaultWateringIntervalDays}d)
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
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
            placeholder="Overrides profile"
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="acquiredDate">Acquired date</Label>
          <Input
            id="acquiredDate"
            type="date"
            required
            value={acquiredDate}
            onChange={(e) => setAcquiredDate(e.target.value)}
          />
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
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="photoUrl">Photo URL</Label>
          <Input
            id="photoUrl"
            value={photoUrl}
            onChange={(e) => setPhotoUrl(e.target.value)}
            placeholder="https://..."
          />
        </div>
      </div>

      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      <div className="flex gap-2 pt-2">
        <Button type="submit" disabled={submitting}>
          {submitting ? "Saving..." : submitLabel}
        </Button>
        <Button type="button" variant="ghost" onClick={onCancel} disabled={submitting}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
