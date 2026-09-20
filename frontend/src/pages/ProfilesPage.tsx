import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { usePlantProfiles, useProfileMutations } from "@/hooks/usePlantProfiles";
import { touchButton, touchField } from "@/lib/ui";
import type { LightRequirement, PlantProfile, PlantProfileInput } from "@/api/types";

const LIGHT_OPTIONS: { value: LightRequirement; labelKey: string }[] = [
  { value: "Low", labelKey: "light.level.Low" },
  { value: "Medium", labelKey: "light.level.Medium" },
  { value: "Bright", labelKey: "light.level.Bright" },
  { value: "DirectSun", labelKey: "light.level.DirectSun" },
];

function emptyInput(): PlantProfileInput {
  return {
    commonName: "",
    scientificName: "",
    defaultWateringIntervalDays: 7,
    lightRequirement: "Medium",
    humidityNotes: "",
    careTips: "",
    toxicToPets: false,
    toxicToChildren: false,
    diagnosisChecklist: "",
  };
}

function toInput(profile: PlantProfile): PlantProfileInput {
  return {
    commonName: profile.commonName,
    scientificName: profile.scientificName,
    defaultWateringIntervalDays: profile.defaultWateringIntervalDays,
    lightRequirement: profile.lightRequirement,
    humidityNotes: profile.humidityNotes,
    careTips: profile.careTips,
    toxicToPets: profile.toxicToPets,
    toxicToChildren: profile.toxicToChildren,
    diagnosisChecklist: profile.diagnosisChecklist ?? "",
  };
}

export function ProfilesPage() {
  const { t } = useTranslation();
  const { data: profiles, isPending } = usePlantProfiles();
  const { create, update } = useProfileMutations();
  const [editingId, setEditingId] = useState<number | null>(null);
  const [draft, setDraft] = useState<PlantProfileInput>(emptyInput);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setError(null);
  }, [editingId]);

  const busy = create.isPending || update.isPending;

  function startCreate() {
    setEditingId(-1);
    setDraft(emptyInput());
  }

  function startEdit(profile: PlantProfile) {
    setEditingId(profile.id);
    setDraft(toInput(profile));
  }

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (draft.commonName.trim() === "") {
      setError(t("profiles.nameRequired"));
      return;
    }
    if (editingId === null || editingId === -1) {
      create.mutate(draft);
    } else {
      update.mutate({ id: editingId, input: draft });
    }
  }

  const isEdit = editingId !== null && editingId > 0;

  return (
    <div className="space-y-4">
      <Breadcrumbs items={[{ label: t("common.home"), to: "/" }, { label: t("profiles.title") }]} />
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">{t("profiles.title")}</h1>
        {!isEdit && editingId !== -1 ? (
          <Button onClick={startCreate} className={touchButton}>
            {t("profiles.new")}
          </Button>
        ) : null}
      </div>

      {editingId !== null ? (
        <Card>
          <CardHeader>
            <CardTitle>{isEdit ? t("profiles.editTitle", { id: editingId }) : t("profiles.new")}</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="space-y-1">
                  <Label htmlFor="pfCommon">{t("profiles.commonName")}</Label>
                  <Input
                    id="pfCommon"
                    required
                    value={draft.commonName}
                    onChange={(e) => setDraft({ ...draft, commonName: e.target.value })}
                    className={touchField}
                  />
                </div>
                <div className="space-y-1">
                  <Label htmlFor="pfScientific">{t("profiles.scientificName")}</Label>
                  <Input
                    id="pfScientific"
                    value={draft.scientificName ?? ""}
                    onChange={(e) => setDraft({ ...draft, scientificName: e.target.value })}
                    className={touchField}
                  />
                </div>
                <div className="space-y-1">
                  <Label htmlFor="pfInterval">{t("profiles.interval")}</Label>
                  <Input
                    id="pfInterval"
                    type="number"
                    min={1}
                    required
                    value={draft.defaultWateringIntervalDays}
                    onChange={(e) =>
                      setDraft({ ...draft, defaultWateringIntervalDays: Number(e.target.value) })
                    }
                    className={touchField}
                  />
                </div>
                <div className="space-y-1">
                  <Label htmlFor="pfLight">{t("profiles.lightRequirement")}</Label>
                  <Select
                    value={draft.lightRequirement}
                    onValueChange={(value) =>
                      setDraft({ ...draft, lightRequirement: value as LightRequirement })
                    }
                  >
                    <SelectTrigger id="pfLight" className={touchField}>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {LIGHT_OPTIONS.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {t(option.labelKey)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <div className="space-y-1">
                <Label htmlFor="pfHumidity">{t("profiles.humidityNotes")}</Label>
                <Input
                  id="pfHumidity"
                  value={draft.humidityNotes}
                  onChange={(e) => setDraft({ ...draft, humidityNotes: e.target.value })}
                  className={touchField}
                />
              </div>
              <div className="space-y-1">
                <Label htmlFor="pfTips">{t("profiles.careTips")}</Label>
                <textarea
                  id="pfTips"
                  rows={6}
                  value={draft.careTips}
                  onChange={(e) => setDraft({ ...draft, careTips: e.target.value })}
                  className={`w-full rounded-md border border-input bg-transparent p-3 text-base sm:text-sm ${touchField}`}
                />
              </div>
              <div className="space-y-1">
                <Label htmlFor="pfChecklist">{t("profiles.checklist")}</Label>
                <textarea
                  id="pfChecklist"
                  rows={4}
                  value={draft.diagnosisChecklist ?? ""}
                  onChange={(e) => setDraft({ ...draft, diagnosisChecklist: e.target.value })}
                  placeholder={t("profiles.checklistPlaceholder")}
                  className={`w-full rounded-md border border-input bg-transparent p-3 font-mono text-xs ${touchField}`}
                />
              </div>

              <div className="flex flex-wrap gap-4">
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={draft.toxicToPets ?? false}
                    onChange={(e) => setDraft({ ...draft, toxicToPets: e.target.checked })}
                    className="h-4 w-4 rounded border-input accent-primary"
                  />
                  {t("profiles.toxicToPets")}
                </label>
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={draft.toxicToChildren ?? false}
                    onChange={(e) => setDraft({ ...draft, toxicToChildren: e.target.checked })}
                    className="h-4 w-4 rounded border-input accent-primary"
                  />
                  {t("profiles.toxicToChildren")}
                </label>
              </div>

              {error ? <p className="text-sm text-destructive">{error}</p> : null}
              <div className="flex gap-2">
                <Button type="submit" disabled={busy}>
                  {busy ? t("common.saving") : isEdit ? t("profiles.saveChanges") : t("profiles.create")}
                </Button>
                <Button type="button" variant="ghost" disabled={busy} onClick={() => setEditingId(null)}>
                  {t("common.cancel")}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      ) : null}

      {isPending ? (
        <div className="space-y-2">
          {Array.from({ length: 4 }, (_, i) => (
            <Skeleton key={i} className="h-20" />
          ))}
        </div>
      ) : (
        <ul className="space-y-3">
          {(profiles ?? []).map((profile) => (
            <li key={profile.id}>
              <Card>
                <CardContent className="flex flex-wrap items-center gap-x-4 gap-y-1 p-4 text-sm">
                  <div className="min-w-40">
                    <span className="font-semibold">{profile.commonName}</span>
                    {profile.scientificName ? (
                      <span className="ml-2 italic text-muted-foreground">{profile.scientificName}</span>
                    ) : null}
                  </div>
                  <span className="text-muted-foreground">{t("profiles.waterInterval", { days: profile.defaultWateringIntervalDays })}</span>
                  <span className="text-muted-foreground">{t(`light.level.${profile.lightRequirement}`)}</span>
                  {profile.toxicToPets ? (
                    <span className="rounded bg-destructive/15 px-1.5 py-0.5 text-xs font-medium text-destructive">
                      {t("profiles.toxicToPets")}
                    </span>
                  ) : null}
                  {profile.toxicToChildren ? (
                    <span className="rounded bg-destructive/15 px-1.5 py-0.5 text-xs font-medium text-destructive">
                      {t("profiles.toxicToChildren")}
                    </span>
                  ) : null}
                  <span className="text-muted-foreground">
                    {t("room.plantCount", { count: profile.plantCount })}
                  </span>
                  <Button
                    size="sm"
                    variant="outline"
                    className="ml-auto"
                    onClick={() => startEdit(profile)}
                  >
                    {t("common.edit")}
                  </Button>
                </CardContent>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default ProfilesPage;
