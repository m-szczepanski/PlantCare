import { useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PhotoPicker } from "@/components/PhotoPicker";
import { ProfileCombobox } from "@/components/ProfileCombobox";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { usePlantProfiles } from "@/hooks/usePlantProfiles";
import { useSoilTypes } from "@/hooks/useSoilTypes";
import { useCreateRoom, useRooms } from "@/hooks/useRooms";
import { applySoilFactor } from "@/lib/soilTypes";
import { touchButton, touchField } from "@/lib/ui";
import type { Plant, PlantInput, SoilType } from "@/api/types";

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
  onSubmit: (input: PlantInput, photoFile: File | null) => void;
  onCancel: () => void;
}

export function PlantForm({ initial, submitting, error, fieldErrors = {}, submitLabel, onSubmit, onCancel }: PlantFormProps) {
  const { t, i18n } = useTranslation();
  const { data: profiles = [] } = usePlantProfiles();
  const { data: soilTypes = [] } = useSoilTypes();
  const { data: rooms = [] } = useRooms();
  const createRoom = useCreateRoom();

  const [nickName, setNickName] = useState(initial?.nickName ?? "");
  const [roomId, setRoomId] = useState<number | null>(initial?.roomId ?? null);
  const [newRoomName, setNewRoomName] = useState("");
  const [photoUrl, setPhotoUrl] = useState(initial?.photoUrl ?? "");
  const [photoFile, setPhotoFile] = useState<File | null>(null);
  const [potSizeCm, setPotSizeCm] = useState(initial?.potSizeCm?.toString() ?? "");
  const [soilType, setSoilType] = useState<SoilType | null>(initial?.soilType ?? null);
  const [propagatedFrom, setPropagatedFrom] = useState(initial?.propagatedFrom ?? "");
  const [notifyEnabled, setNotifyEnabled] = useState<boolean>(initial?.notifyEnabled ?? true);
  const [profileId, setProfileId] = useState<number | null>(initial?.plantProfileId ?? null);
  const [customInterval, setCustomInterval] = useState<string>(
    initial?.customWateringIntervalDays?.toString() ?? "",
  );
  const [acquiredDate, setAcquiredDate] = useState<string>(toDateValue(initial?.acquiredDate) || toDateValue(new Date().toISOString()));
  const [lastWateredAt, setLastWateredAt] = useState<string>(toDateValue(initial?.lastWateredAt));
  const [reduceInWinter, setReduceInWinter] = useState<boolean>(initial?.reduceInWinter ?? false);

  const selectedProfile = profiles.find((profile) => profile.id === profileId);

  const customDays = customInterval.trim() === "" ? null : Number(customInterval);
  const baseInterval =
    customDays !== null && Number.isFinite(customDays)
      ? customDays
      : selectedProfile?.defaultWateringIntervalDays ?? null;
  const effectiveInterval = applySoilFactor(soilTypes, soilType, baseInterval);

  const nextDuePreview = (() => {
    if (effectiveInterval === null) {
      return t("form.noSchedulePreview");
    }
    const base = lastWateredAt ? new Date(`${lastWateredAt}T00:00:00`) : new Date();
    const due = new Date(base.getTime() + effectiveInterval * 86_400_000);
    const dueMidnight = new Date(due.getFullYear(), due.getMonth(), due.getDate());
    const daysFromToday = Math.round(
      (dueMidnight.getTime() - new Date().setHours(0, 0, 0, 0)) / 86_400_000,
    );
    return t("form.nextDuePreview", { date: due.toLocaleDateString(i18n.language), when: daysFromToday === 0 ? t("form.today") : t("form.inDays", { count: daysFromToday }) });
  })();

  const soilHint = (() => {
    if (!soilType || baseInterval === null || effectiveInterval === null || effectiveInterval === baseInterval) {
      return null;
    }
    return t("form.soilTypeHint", {
      soil: t(`soilType.${soilType}`),
      from: baseInterval,
      to: effectiveInterval,
    });
  })();

  const soilMixChoice = (value: string): SoilType | null => {
    if (value === "none") return null;
    const named = soilTypes.find((option) => option.mixes.includes(value));
    return named ? named.type : (value as SoilType);
  };

  function handleSubmit(event: FormEvent) {
    event.preventDefault();

    const trimmedInterval = customInterval.trim();
    const intervalDays = trimmedInterval === "" ? null : Number(trimmedInterval);

    onSubmit(
      {
        nickName: nickName.trim(),
        roomId,
        photoUrl: photoUrl.trim() || null,
        potSizeCm: potSizeCm.trim() === "" ? null : Number(potSizeCm),
        soilType,
        propagatedFrom: propagatedFrom.trim() || null,
        notifyEnabled,
        acquiredDate: fromDateString(acquiredDate) ?? new Date().toISOString(),
        plantProfileId: profileId,
        customWateringIntervalDays: intervalDays !== null && Number.isFinite(intervalDays) ? intervalDays : null,
        reduceInWinter,
        lastWateredAt: fromDateString(lastWateredAt),
      },
      photoFile,
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="nickName">{t("form.nickName")}</Label>
        <Input
          id="nickName"
          required
          value={nickName}
          onChange={(e) => setNickName(e.target.value)}
          placeholder={t("form.nickNamePlaceholder")}
          aria-invalid={fieldErrors.nickName ? true : undefined}
          className={touchField}
        />
        <FieldError message={fieldErrors.nickName} />
      </div>

      <div className="space-y-2">
        <Label htmlFor="room">{t("form.room")}</Label>
        <Select
          value={roomId == null ? "none" : String(roomId)}
          onValueChange={(value) => setRoomId(value === "none" ? null : Number(value))}
        >
          <SelectTrigger id="room" className={touchField}>
            <SelectValue placeholder={t("form.noRoom")} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="none">{t("form.noRoom")}</SelectItem>
            {rooms.map((room) => (
              <SelectItem key={room.id} value={String(room.id)}>
                {room.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <div className="flex gap-2">
          <Input
            aria-label={t("form.newRoomAria")}
            value={newRoomName}
            onChange={(e) => setNewRoomName(e.target.value)}
            placeholder={t("form.quickAddRoom")}
            className={touchField}
          />
          <Button
            type="button"
            variant="outline"
            disabled={newRoomName.trim() === "" || createRoom.isPending}
            onClick={() =>
              createRoom.mutate(
                { name: newRoomName.trim() },
                { onSuccess: (room) => { setRoomId(room.id); setNewRoomName(""); } },
              )
            }
          >
            {createRoom.isPending ? t("common.adding") : t("common.add")}
          </Button>
        </div>
        <FieldError message={fieldErrors.roomId} />
      </div>

      <div className="space-y-2">
        <Label htmlFor="profile">{t("form.speciesProfile")}</Label>
        <ProfileCombobox id="profile" profiles={profiles} value={profileId} onChange={setProfileId} />
        <FieldError message={fieldErrors.plantProfileId} />
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor="customInterval">{t("form.customInterval")}</Label>
          <Input
            id="customInterval"
            type="number"
            min={1}
            value={customInterval}
            onChange={(e) => setCustomInterval(e.target.value)}
            placeholder={selectedProfile ? t("form.profileDefault", { days: selectedProfile.defaultWateringIntervalDays }) : t("form.overridesProfile")}
            aria-invalid={fieldErrors.customWateringIntervalDays ? true : undefined}
            className={touchField}
          />
          <FieldError message={fieldErrors.customWateringIntervalDays} />
        </div>

        <div className="space-y-2">
          <Label htmlFor="acquiredDate">{t("form.acquiredDate")}</Label>
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

      <div className="space-y-4">
        <div className="space-y-2">
          <Label>{t("form.photo")}</Label>
          <PhotoPicker
            currentUrl={photoUrl.trim() || initial?.photoUrl || null}
            nickName={nickName.trim() || t("form.newPlant")}
            value={photoFile}
            onChange={setPhotoFile}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="photoUrl">{t("form.photoUrl")}</Label>
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

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor="lastWateredAt">{t("form.lastWatered")}</Label>
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
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor="potSizeCm">{t("form.potSize")}</Label>
          <Input
            id="potSizeCm"
            type="number"
            min={1}
            max={200}
            value={potSizeCm}
            onChange={(e) => setPotSizeCm(e.target.value)}
            placeholder="14"
            aria-invalid={fieldErrors.potSizeCm ? true : undefined}
            className={touchField}
          />
          <FieldError message={fieldErrors.potSizeCm} />
        </div>
        <div className="space-y-2">
          <Label htmlFor="soilType">{t("form.soilType")}</Label>
          <Select
            value={soilType ?? "none"}
            onValueChange={(value) => setSoilType(soilMixChoice(value))}
          >
            <SelectTrigger id="soilType" className={touchField}>
              <SelectValue placeholder={t("form.soilTypeNone")} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="none">{t("form.soilTypeNone")}</SelectItem>
              {soilTypes.map((option) => (
                <SelectGroup key={option.type}>
                  <SelectItem value={option.type}>{t(`soilType.${option.type}`)}</SelectItem>
                  {option.mixes
                    .filter((mix) => mix !== t(`soilType.${option.type}`))
                    .map((mix) => (
                      <SelectItem key={mix} value={mix} className="text-muted-foreground">
                        {mix}
                      </SelectItem>
                    ))}
                </SelectGroup>
              ))}
            </SelectContent>
          </Select>
          {soilHint ? <p className="text-xs text-muted-foreground">{soilHint}</p> : null}
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="propagatedFrom">{t("form.propagatedFrom")}</Label>
        <Input
          id="propagatedFrom"
          value={propagatedFrom}
          onChange={(e) => setPropagatedFrom(e.target.value)}
          placeholder={t("form.propagatedFromPlaceholder")}
          className={touchField}
        />
      </div>

      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={notifyEnabled}
          onChange={(e) => setNotifyEnabled(e.target.checked)}
          className="h-4 w-4 shrink-0 rounded border-input accent-primary"
        />
        {t("form.notifyReminders")}
      </label>

      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={reduceInWinter}
          onChange={(e) => setReduceInWinter(e.target.checked)}
          className="h-4 w-4 shrink-0 rounded border-input accent-primary"
        />
        {t("form.reduceWinter")}
      </label>

      <p
        className="rounded-md bg-muted px-3 py-2 text-sm text-muted-foreground"
        role="status"
      >
        {nextDuePreview}
      </p>

      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      <div className="flex flex-wrap gap-2 pt-2">
        <Button type="submit" disabled={submitting} className={touchButton}>
          {submitting ? t("common.saving") : submitLabel}
        </Button>
        <Button type="button" variant="ghost" onClick={onCancel} disabled={submitting} className={touchButton}>
          {t("common.cancel")}
        </Button>
      </div>
    </form>
  );
}

function FieldError({ message }: { message?: string }) {
  if (!message) return null;
  return <p className="text-sm text-destructive">{message}</p>;
}
