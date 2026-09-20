import { useState } from "react";
import { Armchair, Droplets, Sun, Thermometer } from "lucide-react";
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

const ORIENTATIONS: { value: RoomOrientation | "none"; label: string }[] = [
  { value: "none", label: "No orientation" },
  { value: "North", label: "North" },
  { value: "East", label: "East" },
  { value: "South", label: "South" },
  { value: "West", label: "West" },
];

function orientationLabel(orientation: RoomOrientation | null): string {
  return orientation ? `${orientation}-facing` : "No orientation set";
}

export function RoomsPage() {
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
      <Breadcrumbs items={[{ label: "Home", to: "/" }, { label: "Rooms" }]} />
      <h1 className="text-2xl font-bold">Rooms</h1>

      <div className="flex flex-wrap gap-2">
        <Input
          aria-label="New room name"
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="e.g. Bedroom"
          className={`w-full sm:w-64 ${touchField}`}
        />
        <Button onClick={handleCreate} disabled={name.trim() === "" || create.isPending} className={touchButton}>
          {create.isPending ? "Adding..." : "Add room"}
        </Button>
      </div>

      {isPending ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 3 }, (_, index) => (
            <Skeleton key={index} className="h-24" />
          ))}
        </div>
      ) : !rooms || rooms.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center px-6 py-10 text-center">
            <div className="rounded-full bg-muted p-3" aria-hidden="true">
              <Armchair className="h-6 w-6 text-muted-foreground" />
            </div>
            <h3 className="mt-3 text-lg font-semibold">No rooms yet</h3>
            <p className="mt-1 max-w-sm text-sm text-muted-foreground">
              Group your plants by room to see what lives where.
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
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
  const update = useUpdateRoom();
  const remove = useDeleteRoom();
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [name, setName] = useState(room.name);
  const [orientation, setOrientation] = useState<RoomOrientation | "none">(room.orientation ?? "none");
  const [light, setLight] = useState<LightRequirement | "none">(room.lightExposure ?? "none");
  const [humidity, setHumidity] = useState<HumidityLevel | "none">(room.humidity ?? "none");
  const [temperature, setTemperature] = useState<string>(
    room.temperatureCelsius === null ? "" : String(room.temperatureCelsius),
  );

  const dirty =
    name.trim() !== room.name ||
    orientation !== (room.orientation ?? "none") ||
    light !== (room.lightExposure ?? "none") ||
    humidity !== (room.humidity ?? "none") ||
    temperature !== (room.temperatureCelsius === null ? "" : String(room.temperatureCelsius));

  return (
    <Card>
      <CardHeader>
        <CardTitle>{room.name}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3 text-sm">
        <p className="text-muted-foreground">
          {orientationLabel(room.orientation)} · {room.plantCount}{" "}
          {room.plantCount === 1 ? "plant" : "plants"}
        </p>
        {room.lightExposure || room.humidity || room.temperatureCelsius !== null ? (
          <p className="flex flex-wrap gap-3 text-xs text-muted-foreground">
            {room.lightExposure ? (
              <span className="flex items-center gap-1">
                <Sun className="h-3 w-3" aria-hidden="true" /> {room.lightExposure}
              </span>
            ) : null}
            {room.humidity ? (
              <span className="flex items-center gap-1">
                <Droplets className="h-3 w-3" aria-hidden="true" /> {room.humidity} humidity
              </span>
            ) : null}
            {room.temperatureCelsius !== null ? (
              <span className="flex items-center gap-1">
                <Thermometer className="h-3 w-3" aria-hidden="true" /> {room.temperatureCelsius}°C
              </span>
            ) : null}
          </p>
        ) : null}
        {plants.length > 0 ? (
          <ul className="space-y-1 text-sm">
            {plants.map((plant) => (
              <li key={plant.id} className="flex items-center justify-between gap-2">
                <span>{plant.nickName}</span>
                {plant.roomLightMatch ? <RoomLightBadge match={plant.roomLightMatch} /> : null}
              </li>
            ))}
          </ul>
        ) : null}
        <div className="space-y-1">
          <Label htmlFor={`room-name-${room.id}`}>Name</Label>
          <Input
            id={`room-name-${room.id}`}
            value={name}
            onChange={(event) => setName(event.target.value)}
            className={touchField}
          />
        </div>
        <div className="grid grid-cols-2 gap-2">
          <div className="space-y-1">
            <Label htmlFor={`room-light-${room.id}`}>Light exposure</Label>
            <Select value={light} onValueChange={(value) => setLight(value as LightRequirement | "none")}>
              <SelectTrigger id={`room-light-${room.id}`} className={touchField}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="none">Unknown</SelectItem>
                {LIGHT_EXPOSURES.map((option) => (
                  <SelectItem key={option} value={option}>
                    {option}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-1">
            <Label htmlFor={`room-humidity-${room.id}`}>Humidity</Label>
            <Select value={humidity} onValueChange={(value) => setHumidity(value as HumidityLevel | "none")}>
              <SelectTrigger id={`room-humidity-${room.id}`} className={touchField}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="none">Unknown</SelectItem>
                {HUMIDITIES.map((option) => (
                  <SelectItem key={option} value={option}>
                    {option}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>
        <div className="space-y-1">
          <Label htmlFor={`room-temperature-${room.id}`}>Average temperature (°C)</Label>
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
          <Label htmlFor={`room-orientation-${room.id}`}>Orientation</Label>
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
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button
            size="sm"
            disabled={!dirty || update.isPending || name.trim() === ""}
            onClick={() =>
              update.mutate({
                id: room.id,
                input: {
                  name: name.trim(),
                  orientation: orientation === "none" ? null : orientation,
                  lightExposure: light === "none" ? null : light,
                  humidity: humidity === "none" ? null : humidity,
                  temperatureCelsius: temperature.trim() === "" ? null : Number(temperature),
                },
              })
            }
          >
            Save
          </Button>
          <AlertDialog open={confirmDelete} onOpenChange={setConfirmDelete}>
            <AlertDialogTrigger asChild>
              <Button size="sm" variant="outline" disabled={remove.isPending}>
                Delete
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Delete {room.name}?</AlertDialogTitle>
                <AlertDialogDescription>
                  The room is removed; its {room.plantCount}{" "}
                  {room.plantCount === 1 ? "plant has" : "plants have"} no room until you pick a new one.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction
                  className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                  onClick={() => {
                    setConfirmDelete(false);
                    remove.mutate(room.id);
                  }}
                >
                  Delete
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
