import { useState, useRef } from "react";
import { useTranslation } from "react-i18next";
import { Link, useNavigate, useParams } from "react-router-dom";
import { toast } from "sonner";
import { AlertTriangle, Droplets } from "lucide-react";
import { CareTasksCard } from "@/components/CareTasksCard";
import { DiagnosticsCard } from "@/components/DiagnosticsCard";
import { JournalCard } from "@/components/JournalCard";
import { CareTipsCard } from "@/components/CareTipsCard";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { DueStatusBadge } from "@/components/DueStatusBadge";
import { EmptyState } from "@/components/EmptyState";
import { PlantDetailSkeleton } from "@/components/PlantDetailSkeleton";
import { RoomLightBadge } from "@/components/RoomLightBadge";
import { PlantPhoto } from "@/components/PlantPhoto";
import { PHOTO_MAX_BYTES, photoFileError } from "@/components/PhotoPicker";
import { WateringHistoryChart } from "@/components/WateringHistoryChart";
import { ApiError } from "@/api/client";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
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
import {
  useAddPlantNote,
  useClearSoilWet,
  useDeletePlant,
  usePlant,
  usePlantNotes,
  useSetSoilWet,
  useSnoozePlant,
  useUnsnoozePlant,
  useUploadPlantPhoto,
  useWaterPlant,
  useWateringLogs,
} from "@/hooks/usePlants";
import type { WaterDetails, WateringMethod } from "@/api/types";
import { touchButton, touchField } from "@/lib/ui";
import { formatInstant } from "@/lib/dates";

function formatDate(value: string | null, locale: string): string {
  if (!value) return "-";
  return new Date(value).toLocaleDateString(locale);
}

export function PlantDetailPage() {
  const { t, i18n } = useTranslation();
  const { id } = useParams();
  const plantId = Number(id);
  const navigate = useNavigate();
  const { data: plant, isPending, isError, error } = usePlant(plantId);
  const deletePlant = useDeletePlant();
  const waterPlant = useWaterPlant();
  const uploadPhoto = useUploadPlantPhoto(plantId);
  const { data: wateringLogs } = useWateringLogs(plantId);
  const { data: notes } = usePlantNotes(plantId);
  const addNote = useAddPlantNote(plantId);
  const snooze = useSnoozePlant(plantId);
  const unsnooze = useUnsnoozePlant(plantId);
  const setSoilWet = useSetSoilWet(plantId);
  const clearSoilWet = useClearSoilWet(plantId);
  const [snoozeDays, setSnoozeDays] = useState("14");
  const [soilWetDays, setSoilWetDays] = useState("3");
  const [noteText, setNoteText] = useState("");
  const photoInputRef = useRef<HTMLInputElement>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [note, setNote] = useState("");
  const [amount, setAmount] = useState("");
  const [method, setMethod] = useState<WateringMethod | "none">("none");

  if (isPending) {
    return <PlantDetailSkeleton />;
  }

  if (isError || !plant) {
    if (error instanceof ApiError && error.status === 404) {
      return (
        <div className="mx-auto max-w-xl">
          <EmptyState
            icon={AlertTriangle}
            title={t("detail.notFoundTitle")}
            description={t("detail.notFoundDescription")}
            action={
              <Button asChild variant="outline">
                <Link to="/plants">{t("detail.backToPlants")}</Link>
              </Button>
            }
          />
        </div>
      );
    }
    return (
      <Card>
        <CardContent className="pt-6 text-destructive">
          {t("detail.loadError", { message: (error as Error)?.message ?? t("common.unknownError") })}
        </CardContent>
      </Card>
    );
  }

  function waterNow() {
    const details: WaterDetails = {};
    if (note.trim() !== "") details.note = note.trim();
    const ml = Number(amount);
    if (amount.trim() !== "" && Number.isFinite(ml)) details.amountMilliliters = ml;
    if (method !== "none") details.method = method;
    waterPlant.mutate({ id: plant!.id, ...details });
    setNote("");
    setAmount("");
    setMethod("none");
  }

  async function handleDelete() {
    setDeleteDialogOpen(false);
    try {
      await deletePlant.mutateAsync(plant!.id);
      navigate("/plants");
    } catch {
      // the hook already surfaced the failure toast
    }
  }

  return (
    <div className="space-y-4">
      <Breadcrumbs
        items={[{ label: t("common.home"), to: "/" }, { label: t("plants.title"), to: "/plants" }, { label: plant.nickName }]}
      />
      <div className="space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h1 className="text-2xl font-bold break-words">{plant.nickName}</h1>
          <DueStatusBadge plant={plant} />
        </div>

        <div className="flex flex-wrap gap-2">
          <input
            ref={photoInputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp,image/gif"
            className="hidden"
            onChange={(event) => {
              const file = event.target.files?.[0];
              event.target.value = "";
              if (!file) return;
              const errorKey = photoFileError(file);
              if (errorKey) {
                toast.error(i18n.t("toasts.photoUploadFailed"), { description: i18n.t(errorKey, { max: PHOTO_MAX_BYTES / 1024 / 1024 }) });
                return;
              }
              uploadPhoto.mutate(file);
            }}
          />
          <Button onClick={waterNow} disabled={waterPlant.isPending} className={touchButton}>
            {waterPlant.isPending ? t("plant.watering") : t("detail.markWatered")}
          </Button>
          <Button variant="secondary" onClick={() => photoInputRef.current?.click()} disabled={uploadPhoto.isPending} className={touchButton}>
            {uploadPhoto.isPending ? t("detail.uploading") : plant.photoUrl ? t("detail.changePhoto") : t("detail.uploadPhoto")}
          </Button>
          <Button asChild className={touchButton}>
            <Link to={`/plants/${plant.id}/edit`}>{t("common.edit")}</Link>
          </Button>
          <Button variant="outline" asChild className={touchButton}>
            <Link to="/plants">{t("detail.backToList")}</Link>
          </Button>
          <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
            <AlertDialogTrigger asChild>
              <Button variant="destructive" disabled={deletePlant.isPending} className={touchButton}>
                {deletePlant.isPending ? t("detail.deleting") : t("common.delete")}
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>{t("detail.deleteTitle", { name: plant.nickName })}</AlertDialogTitle>
                <AlertDialogDescription>
                  {t("detail.deleteDescription")}
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>{t("common.cancel")}</AlertDialogCancel>
                <AlertDialogAction
                  className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                  onClick={handleDelete}
                >
                  {t("common.delete")}
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </div>
      </div>

      <div className="flex flex-col gap-4 lg:flex-row">
        <Card className="order-2 min-w-0 flex-1 lg:order-1">
          <CardHeader>
            <CardTitle>{t("detail.title")}</CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 gap-4 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-muted-foreground">{t("form.room")}</dt>
            <dd className="flex flex-wrap items-center gap-2 font-medium">
              {plant.roomName ?? t("detail.none")}
              {plant.roomLightMatch ? <RoomLightBadge match={plant.roomLightMatch} /> : null}
            </dd>
          </div>
          <Field label={t("form.speciesProfile")} value={plant.profileCommonName ?? t("detail.none")} />
          <Field label={t("detail.acquired")} value={formatDate(plant.acquiredDate, i18n.language)} />
          <Field label={t("form.lastWatered")} value={formatInstant(plant.lastWateredAt, false)} />
          <Field label={t("detail.potSize")} value={plant.potSizeCm ? t("detail.potSizeValue", { cm: plant.potSizeCm }) : t("room.unknown")} />
          <Field label={t("detail.soilType")} value={plant.soilType ? t(`soilType.${plant.soilType}`) : t("detail.notRecorded")} />
          <Field label={t("form.soilMix")} value={plant.soilMix ?? t("detail.notRecorded")} />
          <Field label={t("form.propagatedFrom")} value={plant.propagatedFrom ?? t("detail.notRecorded")} />
          <Field label={t("detail.wateringInterval")} value={plant.wateringIntervalDays ? t("common.days", { count: plant.wateringIntervalDays }) : t("due.notScheduled")} />
          <Field label={t("detail.nextDue")} value={formatDate(plant.nextDueDate, i18n.language)} />
          <Field label={t("detail.reminders")} value={plant.notifyEnabled ? t("detail.on") : t("detail.muted")} />
          <div>
            <dt className="text-muted-foreground">{t("detail.snooze")}</dt>
            <dd className="flex flex-wrap items-center gap-2">
              {plant.snoozedUntil && new Date(plant.snoozedUntil) > new Date() ? (
                <>
                  <span className="text-muted-foreground">
                    {t("detail.onHoldUntil", { date: new Date(plant.snoozedUntil).toLocaleDateString(i18n.language) })}
                  </span>
                  <Button
                    size="sm"
                    variant="outline"
                    disabled={unsnooze.isPending}
                    onClick={() => unsnooze.mutate()}
                  >
                    {t("detail.resumeNow")}
                  </Button>
                </>
              ) : (
                <>
                  <select
                    aria-label={t("detail.snoozeDaysAria")}
                    value={snoozeDays}
                    onChange={(event) => setSnoozeDays(event.target.value)}
                    className="h-9 rounded-md border border-input bg-transparent px-2 text-sm"
                  >
                    {[3, 7, 14, 30].map((days) => (
                      <option key={days} value={String(days)}>{t("common.days", { count: days })}</option>
                    ))}
                  </select>
                  <Button
                    size="sm"
                    variant="outline"
                    disabled={snooze.isPending}
                    onClick={() => snooze.mutate(Number(snoozeDays))}
                  >
                    {t("detail.snoozeReminders")}
                  </Button>
                </>
              )}
            </dd>
          </div>

          <div>
            <dt className="text-muted-foreground">{t("detail.soilWet")}</dt>
            <dd className="flex flex-wrap items-center gap-2">
              {plant.soilWetUntil && new Date(plant.soilWetUntil) > new Date() ? (
                <>
                  <span className="text-muted-foreground">
                    {t("detail.soilWetUntil", { date: new Date(plant.soilWetUntil).toLocaleDateString(i18n.language) })}
                  </span>
                  <Button
                    size="sm"
                    variant="outline"
                    disabled={clearSoilWet.isPending}
                    onClick={() => clearSoilWet.mutate()}
                  >
                    {t("detail.soilWetCheckNow")}
                  </Button>
                </>
              ) : (
                <>
                  <select
                    aria-label={t("detail.soilWetDaysAria")}
                    value={soilWetDays}
                    onChange={(event) => setSoilWetDays(event.target.value)}
                    className="h-9 rounded-md border border-input bg-transparent px-2 text-sm"
                  >
                    {[2, 3, 5, 7].map((days) => (
                      <option key={days} value={String(days)}>{t("common.days", { count: days })}</option>
                    ))}
                  </select>
                  <Button
                    size="sm"
                    variant="outline"
                    disabled={setSoilWet.isPending}
                    onClick={() => setSoilWet.mutate(Number(soilWetDays))}
                  >
                    {t("detail.soilWetMark")}
                  </Button>
                </>
              )}
            </dd>
          </div>
          </CardContent>
        </Card>

        <PlantPhoto
          photoUrl={plant.photoUrl}
          nickName={plant.nickName}
          className="order-1 w-full max-w-xs self-center rounded-lg shadow-sm aspect-[3/4] sm:w-64 lg:order-2 lg:w-72 lg:max-w-none lg:self-stretch lg:aspect-auto lg:h-full xl:w-80"
        />
      </div>

      {plant.profileToxicToPets || plant.profileToxicToChildren ? (
        <div className="flex flex-wrap gap-2" role="status">
          {plant.profileToxicToPets ? (
            <Badge variant="destructive" className="gap-1">
              <AlertTriangle className="h-3 w-3" aria-hidden="true" />
              {t("plant.toxicToPets")}
            </Badge>
          ) : null}
          {plant.profileToxicToChildren ? (
            <Badge variant="destructive" className="gap-1">
              <AlertTriangle className="h-3 w-3" aria-hidden="true" />
              {t("plant.toxicToChildren")}
            </Badge>
          ) : null}
        </div>
      ) : null}

      {plant.careTips ? <CareTipsCard tips={plant.careTips} /> : null}
      {plant.careTips?.diagnosisChecklist ? (
        <DiagnosticsCard
          checklist={plant.careTips.diagnosisChecklist}
          commonName={plant.careTips.commonName}
        />
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>{t("detail.wateringHistory")}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {wateringLogs && wateringLogs.length > 0 ? (
            <>
              <WateringHistoryChart logs={wateringLogs} />
              <ul className="space-y-1 text-sm">
                {wateringLogs.map((log) => (
                  <li key={log.id} className="flex flex-wrap items-baseline gap-2">
                    <span className="font-medium">{formatInstant(log.wateredAt, true)}</span>
                    {log.amountMilliliters ? (
                      <span className="text-muted-foreground">{log.amountMilliliters} ml</span>
                    ) : null}
                    {log.method ? <span className="text-muted-foreground">{t(`water.method.${log.method}`)}</span> : null}
                    {log.note ? <span className="text-muted-foreground">{log.note}</span> : null}
                  </li>
                ))}
              </ul>
            </>
          ) : (
            <EmptyState
              icon={Droplets}
              title={t("detail.noWateringsTitle")}
              description={t("detail.noWateringsDescription")}
            />
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t("detail.logWatering")}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap items-end gap-2">
          <div className="min-w-40 flex-1 space-y-1">
            <Label htmlFor="waterNote">{t("detail.noteOptional")}</Label>
            <Input
              id="waterNote"
              value={note}
              onChange={(event) => setNote(event.target.value)}
              placeholder={t("detail.notePlaceholder")}
              className={touchField}
            />
          </div>
          <div className="w-28 space-y-1">
            <Label htmlFor="waterAmount">{t("detail.amountOptional")}</Label>
            <Input
              id="waterAmount"
              type="number"
              min={1}
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              placeholder="250"
              className={touchField}
            />
          </div>
          <div className="w-36 space-y-1">
            <Label htmlFor="waterMethod">{t("detail.method")}</Label>
            <Select value={method} onValueChange={(value) => setMethod(value as WateringMethod | "none")}>
              <SelectTrigger id="waterMethod" className={touchField}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="none">{t("water.method.none")}</SelectItem>
                <SelectItem value="Tap">{t("water.method.Tap")}</SelectItem>
                <SelectItem value="Filtered">{t("water.method.Filtered")}</SelectItem>
                <SelectItem value="Rainwater">{t("water.method.Rainwater")}</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      <CareTasksCard plantId={plantId} />

      <JournalCard plantId={plantId} nickName={plant.nickName} />

      <Card>
        <CardHeader>
          <CardTitle>{t("detail.notes")}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="flex flex-wrap gap-2">
            <Input
              aria-label={t("detail.newNoteAria")}
              value={noteText}
              onChange={(event) => setNoteText(event.target.value)}
              placeholder={t("detail.noteListPlaceholder")}
              className={`min-w-48 flex-1 ${touchField}`}
            />
            <Button
              disabled={noteText.trim() === "" || addNote.isPending}
              onClick={() =>
                addNote.mutate(noteText.trim(), { onSuccess: () => setNoteText("") })
              }
              className={touchButton}
            >
              {addNote.isPending ? t("common.adding") : t("detail.addNote")}
            </Button>
          </div>
          {!notes || notes.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t("detail.noNotes")}</p>
          ) : (
            <ul className="space-y-2 text-sm">
              {notes.map((note) => (
                <li key={note.id} className="rounded-md border p-3">
                  <p>{note.text}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {formatInstant(note.createdAt)}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="font-medium">{value}</dd>
    </div>
  );
}

export default PlantDetailPage;
