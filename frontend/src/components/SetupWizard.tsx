import { useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { ArrowLeft, ArrowRight, Check, Leaf, Languages, Moon, Sun, X } from "lucide-react";
import { PlantForm } from "@/components/PlantForm";
import { useTheme, type Theme } from "@/components/ThemeProvider";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
  activeLanguage,
  LANGUAGE_LABELS,
  setLanguage,
  SUPPORTED_LANGUAGES,
} from "@/i18n";
import { useCreatePlant, usePlants, useUploadPlantPhotoToId } from "@/hooks/usePlants";
import { useCreateRoom, useDeleteRoom, useRooms } from "@/hooks/useRooms";
import { splitApiError } from "@/lib/validation";
import { touchButton, touchField } from "@/lib/ui";
import type { PlantInput } from "@/api/types";

const STEPS = ["welcome", "rooms", "plants", "preferences", "finish"] as const;

type Step = (typeof STEPS)[number];

export interface SetupWizardProps {
  onFinished: () => void;
}

export function SetupWizard({ onFinished }: SetupWizardProps) {
  const { t } = useTranslation();
  const [step, setStep] = useState<Step>("welcome");
  const stepIndex = STEPS.indexOf(step);
  const { data: rooms = [] } = useRooms();
  const { data: plants = [] } = usePlants();

  const next = () => setStep(STEPS[Math.min(stepIndex + 1, STEPS.length - 1)]);
  const back = () => setStep(STEPS[Math.max(stepIndex - 1, 0)]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/30 p-4">
      <div className="w-full max-w-xl space-y-4">
        <div className="flex items-center justify-between gap-4 px-1">
          <div className="flex items-center gap-2 font-bold">
            <Leaf className="h-5 w-5 text-primary" aria-hidden="true" />
            PlantCare
          </div>
          <p className="text-sm text-muted-foreground" role="status">
            {t("setup.stepOf", { current: stepIndex + 1, total: STEPS.length })}
          </p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>{t(`setup.steps.${step}`)}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {step === "welcome" ? <WelcomeStep /> : null}
            {step === "rooms" ? <RoomsStep rooms={rooms} /> : null}
            {step === "plants" ? <PlantsStep plants={plants.map((p) => p.nickName)} /> : null}
            {step === "preferences" ? <PreferencesStep /> : null}
            {step === "finish" ? (
              <FinishStep roomCount={rooms.length} plantCount={plants.length} onFinished={onFinished} />
            ) : null}

            {step !== "welcome" && step !== "finish" ? (
              <div className="flex items-center justify-between gap-2 pt-2">
                <Button variant="ghost" onClick={back} className={touchButton}>
                  <ArrowLeft className="mr-1 h-4 w-4" aria-hidden="true" />
                  {t("setup.back")}
                </Button>
                <div className="flex items-center gap-2">
                  {step === "rooms" || step === "plants" ? (
                    <Button variant="ghost" onClick={next} className={touchButton}>
                      {t("setup.skip")}
                    </Button>
                  ) : null}
                  <Button onClick={next} className={touchButton}>
                    {t("setup.next")}
                    <ArrowRight className="ml-1 h-4 w-4" aria-hidden="true" />
                  </Button>
                </div>
              </div>
            ) : null}

            {step === "welcome" ? (
              <div className="flex justify-end pt-2">
                <Button onClick={next} className={touchButton}>
                  {t("setup.start")}
                  <ArrowRight className="ml-1 h-4 w-4" aria-hidden="true" />
                </Button>
              </div>
            ) : null}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function WelcomeStep() {
  const { t } = useTranslation();
  return (
    <div className="space-y-2 text-sm">
      <p>{t("setup.welcome.intro")}</p>
      <ul className="list-disc space-y-1 pl-5 text-muted-foreground">
        <li>{t("setup.welcome.itemRooms")}</li>
        <li>{t("setup.welcome.itemPlants")}</li>
        <li>{t("setup.welcome.itemPreferences")}</li>
      </ul>
    </div>
  );
}

function RoomsStep({ rooms }: { rooms: { id: number; name: string }[] }) {
  const { t } = useTranslation();
  const createRoom = useCreateRoom();
  const deleteRoom = useDeleteRoom();
  const [name, setName] = useState("");

  function handleAdd(event: FormEvent) {
    event.preventDefault();
    const trimmed = name.trim();
    if (!trimmed || createRoom.isPending) return;
    createRoom.mutate({ name: trimmed }, { onSuccess: () => setName("") });
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">{t("setup.rooms.description")}</p>
      <form onSubmit={handleAdd} className="flex gap-2">
        <Input
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder={t("rooms.namePlaceholder")}
          aria-label={t("rooms.add")}
          className={touchField}
        />
        <Button type="submit" variant="outline" disabled={name.trim() === "" || createRoom.isPending}>
          {createRoom.isPending ? t("common.adding") : t("common.add")}
        </Button>
      </form>
      {rooms.length === 0 ? (
        <p className="text-sm text-muted-foreground">{t("rooms.emptyTitle")}</p>
      ) : (
        <ul className="space-y-1">
          {rooms.map((room) => (
            <li key={room.id} className="flex items-center justify-between rounded-md border px-3 py-2 text-sm">
              {room.name}
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8"
                aria-label={t("setup.rooms.removeAria", { name: room.name })}
                onClick={() => deleteRoom.mutate(room.id)}
              >
                <X className="h-4 w-4" aria-hidden="true" />
              </Button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function PlantsStep({ plants }: { plants: string[] }) {
  const { t } = useTranslation();
  const createPlant = useCreatePlant();
  const uploadPhoto = useUploadPlantPhotoToId();
  const split = splitApiError(createPlant.error);
  const [formKey, setFormKey] = useState(0);

  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">{t("setup.plants.description")}</p>
      {plants.length > 0 ? (
        <p className="rounded-md bg-muted px-3 py-2 text-sm" role="status">
          {t("setup.plants.added", { count: plants.length, names: plants.join(", ") })}
        </p>
      ) : null}
      {split.banner ? <p className="text-sm text-destructive">{split.banner}</p> : null}
      <PlantForm
        key={formKey}
        submitting={createPlant.isPending}
        fieldErrors={split.fields}
        submitLabel={t("form.addPlant")}
        onSubmit={(input: PlantInput, photoFile: File | null) =>
          createPlant.mutate(input, {
            onSuccess: (plant) => {
              if (photoFile) uploadPhoto.mutate({ id: plant.id, file: photoFile });
              setFormKey((key) => key + 1);
            },
          })
        }
        onCancel={() => setFormKey((key) => key + 1)}
      />
    </div>
  );
}

function PreferencesStep() {
  const { t } = useTranslation();
  const { theme, setTheme } = useTheme();
  const currentLanguage = activeLanguage();

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <p className="flex items-center gap-2 text-sm font-medium">
          <Sun className="h-4 w-4" aria-hidden="true" />
          {t("setup.preferences.theme")}
        </p>
        <div className="flex flex-wrap gap-2" role="group" aria-label={t("setup.preferences.theme")}>
          {(["light", "dark", "system"] as Theme[]).map((option) => (
            <Button
              key={option}
              variant={theme === option ? "default" : "outline"}
              aria-pressed={theme === option}
              onClick={() => setTheme(option)}
              className={touchButton}
            >
              {option === "light" ? (
                <Sun className="mr-1 h-4 w-4" aria-hidden="true" />
              ) : option === "dark" ? (
                <Moon className="mr-1 h-4 w-4" aria-hidden="true" />
              ) : null}
              {t(`theme.${option}`)}
            </Button>
          ))}
        </div>
      </div>
      <div className="space-y-2">
        <p className="flex items-center gap-2 text-sm font-medium">
          <Languages className="h-4 w-4" aria-hidden="true" />
          {t("setup.preferences.language")}
        </p>
        <div className="flex flex-wrap gap-2" role="group" aria-label={t("setup.preferences.language")}>
          {SUPPORTED_LANGUAGES.map((language) => (
            <Button
              key={language}
              variant={currentLanguage === language ? "default" : "outline"}
              aria-pressed={currentLanguage === language}
              onClick={() => {
                if (currentLanguage !== language) void setLanguage(language);
              }}
              className={touchButton}
            >
              {LANGUAGE_LABELS[language]}
            </Button>
          ))}
        </div>
      </div>
    </div>
  );
}

function FinishStep({
  roomCount,
  plantCount,
  onFinished,
}: {
  roomCount: number;
  plantCount: number;
  onFinished: () => void;
}) {
  const { t } = useTranslation();
  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">{t("setup.finish.description")}</p>
      <ul className="space-y-1 text-sm">
        <li className="flex items-center gap-2">
          <Check className="h-4 w-4 text-primary" aria-hidden="true" />
          {t("setup.finish.rooms", { count: roomCount })}
        </li>
        <li className="flex items-center gap-2">
          <Check className="h-4 w-4 text-primary" aria-hidden="true" />
          {t("setup.finish.plants", { count: plantCount })}
        </li>
      </ul>
      <div className="flex justify-end pt-2">
        <Button onClick={onFinished} className={touchButton}>
          {t("setup.finish.goToDashboard")}
        </Button>
      </div>
    </div>
  );
}
