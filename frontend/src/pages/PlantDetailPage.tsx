import { useState, useRef } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { AlertTriangle, Droplets } from "lucide-react";
import { CareTasksCard } from "@/components/CareTasksCard";
import { CareTipsCard } from "@/components/CareTipsCard";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { DueStatusBadge } from "@/components/DueStatusBadge";
import { EmptyState } from "@/components/EmptyState";
import { PlantDetailSkeleton } from "@/components/PlantDetailSkeleton";
import { RoomLightBadge } from "@/components/RoomLightBadge";
import { PlantPhoto } from "@/components/PlantPhoto";
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
  useDeletePlant,
  usePlant,
  usePlantNotes,
  useUploadPlantPhoto,
  useWaterPlant,
  useWateringLogs,
} from "@/hooks/usePlants";
import type { WaterDetails, WateringMethod } from "@/api/types";
import { touchButton, touchField } from "@/lib/ui";

function formatDate(value: string | null): string {
  if (!value) return "-";
  return new Date(value).toLocaleDateString();
}

// The API stores watering instants as naive UTC strings; without the Z suffix
// browsers would read them back as local time and show the wrong moment.
function formatInstant(value: string | null, withTime: boolean): string {
  if (!value) return "-";
  const date = new Date(value.endsWith("Z") ? value : `${value}Z`);
  return withTime ? date.toLocaleString() : date.toLocaleDateString();
}

export function PlantDetailPage() {
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
            title="Plant not found"
            description="It may have been deleted, or the link is out of date."
            action={
              <Button asChild variant="outline">
                <Link to="/plants">Back to plants</Link>
              </Button>
            }
          />
        </div>
      );
    }
    return (
      <Card>
        <CardContent className="pt-6 text-destructive">
          Could not load plant: {(error as Error)?.message ?? "Unknown error"}
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
        items={[{ label: "Home", to: "/" }, { label: "Plants", to: "/plants" }, { label: plant.nickName }]}
      />
      <div className="flex items-center justify-between gap-2">
        <h1 className="text-2xl font-bold break-words">{plant.nickName}</h1>
        <DueStatusBadge plant={plant} />
      </div>

      <PlantPhoto
        photoUrl={plant.photoUrl}
        nickName={plant.nickName}
        className="h-64 w-full sm:h-72"
      />

      <Card>
        <CardHeader>
          <CardTitle>Details</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-muted-foreground">Room</dt>
            <dd className="flex flex-wrap items-center gap-2 font-medium">
              {plant.roomName ?? "None"}
              {plant.roomLightMatch ? <RoomLightBadge match={plant.roomLightMatch} /> : null}
            </dd>
          </div>
          <Field label="Species profile" value={plant.profileCommonName ?? "None"} />
          <Field label="Acquired" value={formatDate(plant.acquiredDate)} />
          <Field label="Last watered" value={formatInstant(plant.lastWateredAt, false)} />
          <Field label="Pot size" value={plant.potSizeCm ? `${plant.potSizeCm} cm` : "Unknown"} />
          <Field label="Soil mix" value={plant.soilMix ?? "Not recorded"} />
          <Field label="Propagated from" value={plant.propagatedFrom ?? "Not recorded"} />
          <Field label="Watering interval" value={plant.wateringIntervalDays ? `${plant.wateringIntervalDays} days` : "Not scheduled"} />
          <Field label="Next due" value={formatDate(plant.nextDueDate)} />
        </CardContent>
      </Card>

      {plant.careTips ? <CareTipsCard tips={plant.careTips} /> : null}

      <Card>
        <CardHeader>
          <CardTitle>Watering history</CardTitle>
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
                    {log.method ? <span className="text-muted-foreground">{log.method}</span> : null}
                    {log.note ? <span className="text-muted-foreground">{log.note}</span> : null}
                  </li>
                ))}
              </ul>
            </>
          ) : (
            <EmptyState
              icon={Droplets}
              title="No waterings logged yet"
              description="Water this plant and the log will show up here."
            />
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Log a watering</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap items-end gap-2">
          <div className="min-w-40 flex-1 space-y-1">
            <Label htmlFor="waterNote">Note (optional)</Label>
            <Input
              id="waterNote"
              value={note}
              onChange={(event) => setNote(event.target.value)}
              placeholder="e.g. Soaked thoroughly"
              className={touchField}
            />
          </div>
          <div className="w-28 space-y-1">
            <Label htmlFor="waterAmount">ml (optional)</Label>
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
            <Label htmlFor="waterMethod">Method</Label>
            <Select value={method} onValueChange={(value) => setMethod(value as WateringMethod | "none")}>
              <SelectTrigger id="waterMethod" className={touchField}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="none">Any water</SelectItem>
                <SelectItem value="Tap">Tap</SelectItem>
                <SelectItem value="Filtered">Filtered</SelectItem>
                <SelectItem value="Rainwater">Rainwater</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      <CareTasksCard plantId={plantId} />

      <Card>
        <CardHeader>
          <CardTitle>Notes</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="flex flex-wrap gap-2">
            <Input
              aria-label="New note"
              value={noteText}
              onChange={(event) => setNoteText(event.target.value)}
              placeholder="Health check, repotting thoughts..."
              className={`min-w-48 flex-1 ${touchField}`}
            />
            <Button
              disabled={noteText.trim() === "" || addNote.isPending}
              onClick={() =>
                addNote.mutate(noteText.trim(), { onSuccess: () => setNoteText("") })
              }
              className={touchButton}
            >
              {addNote.isPending ? "Adding..." : "Add note"}
            </Button>
          </div>
          {!notes || notes.length === 0 ? (
            <p className="text-sm text-muted-foreground">No notes yet.</p>
          ) : (
            <ul className="space-y-2 text-sm">
              {notes.map((note) => (
                <li key={note.id} className="rounded-md border p-3">
                  <p>{note.text}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {new Date(note.createdAt).toLocaleString()}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <div className="flex flex-wrap gap-2">
        <input
          ref={photoInputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp,image/gif"
          className="hidden"
          onChange={(event) => {
            const file = event.target.files?.[0];
            if (file) {
              uploadPhoto.mutate(file);
            }
            event.target.value = "";
          }}
        />
        <Button onClick={waterNow} disabled={waterPlant.isPending} className={touchButton}>
          {waterPlant.isPending ? "Watering..." : "Mark as watered"}
        </Button>
        <Button variant="secondary" onClick={() => photoInputRef.current?.click()} disabled={uploadPhoto.isPending} className={touchButton}>
          {uploadPhoto.isPending ? "Uploading..." : plant.photoUrl ? "Change photo" : "Upload photo"}
        </Button>
        <Button asChild className={touchButton}>
          <Link to={`/plants/${plant.id}/edit`}>Edit</Link>
        </Button>
        <Button variant="outline" asChild className={touchButton}>
          <Link to="/plants">Back to list</Link>
        </Button>
        <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
          <AlertDialogTrigger asChild>
            <Button variant="destructive" disabled={deletePlant.isPending} className={touchButton}>
              {deletePlant.isPending ? "Deleting..." : "Delete"}
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Delete {plant.nickName}?</AlertDialogTitle>
              <AlertDialogDescription>
                This removes the plant and its watering history. This cannot be undone.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Cancel</AlertDialogCancel>
              <AlertDialogAction
                className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                onClick={handleDelete}
              >
                Delete
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
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
