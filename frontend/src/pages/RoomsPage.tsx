import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Armchair, Droplets, Sprout, Sun, Thermometer } from "lucide-react";
import { Link } from "react-router-dom";
import { Breadcrumbs } from "@/components/Breadcrumbs";
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
import { PlantPhoto } from "@/components/PlantPhoto";
import { wateredRelative } from "@/lib/dates";
import {
  useCreateRoom,
  useDeleteRoom,
  useRooms,
  useUpdateRoom,
} from "@/hooks/useRooms";
import { usePlants } from "@/hooks/usePlants";
import { RoomLightBadge } from "@/components/RoomLightBadge";
import type { HumidityLevel, LightRequirement, Plant, Room, RoomOrientation } from "@/api/types";
import { touchButton, touchField } from "@/lib/ui";

const ORIENTATIONS: { value: RoomOrientation | "none"; labelKey: string }[] = [
  { value: "none", labelKey: "room.noOrientation" },
  { value: "North", labelKey: "room.orientation.North" },
  { value: "East", labelKey: "room.orientation.East" },
  { value: "South", labelKey: "room.orientation.South" },
  { value: "West", labelKey: "room.orientation.West" },
];

export function RoomsPage() {
  const { t } = useTranslation();
  const { data: rooms, isPending } = useRooms();
  const { data: plants = [] } = usePlants();
  const [name, setName] = useState("");
  const create = useCreateRoom();

  function handleCreate() {
    const trimmed = name.trim();
    if (trimmed === "") return;
    create.mutate({ name: trimmed }, { onSuccess: () => setName("") });
  }

  return (
    <div className="space-y-4">
      <Breadcrumbs items={[{ label: t("common.home"), to: "/" }, { label: t("rooms.title") }]} />
      <h1 className="text-3xl font-bold">{t("rooms.title")}</h1>

      <div className="flex flex-wrap gap-2">
        <Input
          aria-label={t("form.newRoomAria")}
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder={t("rooms.namePlaceholder")}
          className={`w-full sm:w-64 ${touchField}`}
        />
        <Button onClick={handleCreate} disabled={name.trim() === "" || create.isPending} className={touchButton}>
          {create.isPending ? t("common.adding") : t("rooms.add")}
        </Button>
      </div>

      {isPending ? (
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 3 }, (_, index) => (
            <Skeleton key={index} className="h-56" />
          ))}
        </div>
      ) : !rooms || rooms.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center px-6 py-10 text-center">
            <div className="rounded-full bg-muted p-3" aria-hidden="true">
              <Armchair className="h-6 w-6 text-muted-foreground" />
            </div>
            <h3 className="mt-3 text-lg font-semibold">{t("rooms.emptyTitle")}</h3>
            <p className="mt-1 max-w-sm text-sm text-muted-foreground">
              {t("rooms.emptyDescription")}
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {rooms.map((room) => (
            <RoomCard
              key={room.id}
              room={room}
              plants={plants.filter((plant) => plant.roomId === room.id)}
            />
          ))}
        </div>
      )}
    </div>
  );
}

const LIGHT_EXPOSURES: LightRequirement[] = ["Low", "Medium", "Bright", "DirectSun"];
const HUMIDITIES: HumidityLevel[] = ["Low", "Medium", "High"];

function RoomCard({ room, plants }: { room: Room; plants: Plant[] }) {
  const { t } = useTranslation();
  const update = useUpdateRoom();
  const remove = useDeleteRoom();
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(room.name);
  const [orientation, setOrientation] = useState<RoomOrientation | "none">(room.orientation ?? "none");
  const [light, setLight] = useState<LightRequirement | "none">(room.lightExposure ?? "none");
  const [humidity, setHumidity] = useState<HumidityLevel | "none">(room.humidity ?? "none");
  const [temperature, setTemperature] = useState<string>(
    room.temperatureCelsius === null ? "" : String(room.temperatureCelsius),
  );

  function syncFromRoom() {
    setName(room.name);
    setOrientation(room.orientation ?? "none");
    setLight(room.lightExposure ?? "none");
    setHumidity(room.humidity ?? "none");
    setTemperature(room.temperatureCelsius === null ? "" : String(room.temperatureCelsius));
  }

  function startEdit() {
    syncFromRoom();
    setEditing(true);
  }

  function cancelEdit() {
    syncFromRoom();
    setEditing(false);
  }

  const dirty =
    name.trim() !== room.name ||
    orientation !== (room.orientation ?? "none") ||
    light !== (room.lightExposure ?? "none") ||
    humidity !== (room.humidity ?? "none") ||
    temperature !== (room.temperatureCelsius === null ? "" : String(room.temperatureCelsius));

  function save() {
    if (!dirty) {
      setEditing(false);
      return;
    }
    update.mutate(
      {
        id: room.id,
        input: {
          name: name.trim(),
          orientation: orientation === "none" ? null : orientation,
          lightExposure: light === "none" ? null : light,
          humidity: humidity === "none" ? null : humidity,
          temperatureCelsius: temperature.trim() === "" ? null : Number(temperature),
        },
      },
      { onSuccess: () => setEditing(false) },
    );
  }

  const overdueCount = plants.filter((plant) => plant.dueStatus === "Overdue").length;
  const dueTodayCount = plants.filter((plant) => plant.dueStatus === "DueToday").length;
  const visiblePlants = plants.slice(0, 5);

  return (
    <Card className="flex h-full flex-col overflow-hidden">
      <CardHeader className="pb-3">
        <CardTitle className="text-xl">{room.name}</CardTitle>
        <p className="text-sm text-muted-foreground">
          {room.orientation
            ? t("room.facing", { orientation: t(`room.orientationShort.${room.orientation}`) })
            : t("room.noOrientationSet")}
        </p>
      </CardHeader>
      <CardContent className="flex flex-1 flex-col gap-3 text-sm">
        {!editing ? (
          <>
            <div className="grid grid-cols-3 divide-x rounded-lg border bg-muted/40 text-center">
              <div className="px-2 py-3">
                <div className="text-2xl font-bold leading-none">{room.plantCount}</div>
                <div className="mt-1 text-xs text-muted-foreground">{t("room.stat.plants")}</div>
              </div>
              <div className="px-2 py-3">
                <div
                  className={
                    overdueCount > 0
                      ? "text-2xl font-bold leading-none text-destructive"
                      : "text-2xl font-bold leading-none"
                  }
                >
                  {overdueCount}
                </div>
                <div className="mt-1 text-xs text-muted-foreground">{t("room.stat.overdue")}</div>
              </div>
              <div className="px-2 py-3">
                <div className="text-2xl font-bold leading-none">{dueTodayCount}</div>
                <div className="mt-1 text-xs text-muted-foreground">{t("room.stat.dueToday")}</div>
              </div>
            </div>
            <p className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
              <span className="flex items-center gap-1">
                <Sun className="h-3.5 w-3.5" aria-hidden="true" />
                {room.lightExposure
                  ? t(`roomLight.level.${room.lightExposure}`)
                  : t("room.lightNotSet")}
              </span>
              <span className="flex items-center gap-1">
                <Droplets className="h-3.5 w-3.5" aria-hidden="true" />
                {room.humidity
                  ? t("room.humidityValue", { level: t(`room.humidity.${room.humidity}`) })
                  : t("room.humidityNotSet")}
              </span>
              <span className="flex items-center gap-1">
                <Thermometer className="h-3.5 w-3.5" aria-hidden="true" />
                {room.temperatureCelsius !== null
                  ? `${room.temperatureCelsius}°C`
                  : t("room.temperatureNotSet")}
              </span>
            </p>
            {plants.length > 0 ? (
              <ul className="space-y-1.5">
                {visiblePlants.map((plant) => (
                  <li key={plant.id} className="flex items-center gap-2">
                    <PlantPhoto
                      photoUrl={plant.photoUrl}
                      nickName={plant.nickName}
                      className="h-8 w-8 shrink-0"
                    />
                    <Link
                      to={`/plants/${plant.id}`}
                      className="min-w-0 flex-1 truncate font-medium hover:underline"
                    >
                      {plant.nickName}
                    </Link>
                    {plant.roomLightMatch ? <RoomLightBadge match={plant.roomLightMatch} /> : null}
                    <span className="shrink-0 text-xs text-muted-foreground">
                      {wateredRelative(plant.lastWateredAt)}
                    </span>
                  </li>
                ))}
                {plants.length > visiblePlants.length ? (
                  <li className="text-xs text-muted-foreground">
                    {t("room.morePlants", { count: plants.length - visiblePlants.length })}
                  </li>
                ) : null}
              </ul>
            ) : (
              <div className="flex flex-1 flex-col items-center justify-center gap-2 rounded-lg border border-dashed py-6 text-center">
                <Sprout className="h-6 w-6 text-muted-foreground" aria-hidden="true" />
                <p className="text-sm text-muted-foreground">{t("room.noPlants")}</p>
                <Button asChild size="sm" variant="outline" className={touchButton}>
                  <Link to="/plants">{t("room.browsePlants")}</Link>
                </Button>
              </div>
            )}
          </>
        ) : null}
        {editing ? (
          <div className="space-y-3">
            <div className="space-y-1">
              <Label htmlFor={`room-name-${room.id}`}>{t("room.name")}</Label>
              <Input
                id={`room-name-${room.id}`}
                value={name}
                onChange={(event) => setName(event.target.value)}
                className={touchField}
              />
            </div>
            <div className="grid grid-cols-2 gap-2">
              <div className="space-y-1">
                <Label htmlFor={`room-light-${room.id}`}>{t("room.lightExposure")}</Label>
                <Select value={light} onValueChange={(value) => setLight(value as LightRequirement | "none")}>
                  <SelectTrigger id={`room-light-${room.id}`} className={touchField}>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">{t("room.unknown")}</SelectItem>
                    {LIGHT_EXPOSURES.map((option) => (
                      <SelectItem key={option} value={option}>
                        {t(`roomLight.level.${option}`)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1">
                <Label htmlFor={`room-humidity-${room.id}`}>{t("room.humidityLabel")}</Label>
                <Select value={humidity} onValueChange={(value) => setHumidity(value as HumidityLevel | "none")}>
                  <SelectTrigger id={`room-humidity-${room.id}`} className={touchField}>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">{t("room.unknown")}</SelectItem>
                    {HUMIDITIES.map((option) => (
                      <SelectItem key={option} value={option}>
                        {t(`room.humidity.${option}`)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-1">
              <Label htmlFor={`room-temperature-${room.id}`}>{t("room.temperature")}</Label>
              <Input
                id={`room-temperature-${room.id}`}
                type="number"
                min={-10}
                max={45}
                value={temperature}
                onChange={(event) => setTemperature(event.target.value)}
                className={touchField}
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor={`room-orientation-${room.id}`}>{t("room.orientationLabel")}</Label>
              <Select
                value={orientation}
                onValueChange={(value) => setOrientation(value as RoomOrientation | "none")}
              >
                <SelectTrigger id={`room-orientation-${room.id}`} className={touchField}>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {ORIENTATIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {t(option.labelKey)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        ) : null}
        <div className={`${editing ? "" : "mt-auto "}flex flex-wrap gap-2${editing ? "" : " border-t pt-3"}`}>
          {editing ? (
            <>
              <Button
                size="sm"
                disabled={update.isPending || name.trim() === ""}
                onClick={save}
              >
                {update.isPending ? t("common.saving") : t("common.save")}
              </Button>
              <Button size="sm" variant="ghost" disabled={update.isPending} onClick={cancelEdit}>
                {t("common.cancel")}
              </Button>
            </>
          ) : (
            <Button size="sm" variant="outline" disabled={remove.isPending} onClick={startEdit}>
              {t("common.edit")}
            </Button>
          )}
          <AlertDialog open={confirmDelete} onOpenChange={setConfirmDelete}>
            <AlertDialogTrigger asChild>
              <Button size="sm" variant="outline" disabled={remove.isPending}>
                {t("common.delete")}
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>{t("rooms.deleteTitle", { name: room.name })}</AlertDialogTitle>
                <AlertDialogDescription>
                  {t("rooms.deleteDescription", { count: room.plantCount })}
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>{t("common.cancel")}</AlertDialogCancel>
                <AlertDialogAction
                  className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                  onClick={() => {
                    setConfirmDelete(false);
                    remove.mutate(room.id);
                  }}
                >
                  {t("common.delete")}
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </div>
      </CardContent>
    </Card>
  );
}

export default RoomsPage;
