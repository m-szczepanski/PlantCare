import { useState, useRef } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { AlertTriangle, Droplets } from "lucide-react";
import { CareTipsCard } from "@/components/CareTipsCard";
import { DueStatusBadge } from "@/components/DueStatusBadge";
import { EmptyState } from "@/components/EmptyState";
import { PlantDetailSkeleton } from "@/components/PlantDetailSkeleton";
import { PlantPhoto } from "@/components/PlantPhoto";
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
import { useDeletePlant, usePlant, useUploadPlantPhoto, useWaterPlant, useWateringLogs } from "@/hooks/usePlants";
import { touchButton } from "@/lib/ui";

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
  const photoInputRef = useRef<HTMLInputElement>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

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
          <Field label="Location" value={plant.location} />
          <Field label="Species profile" value={plant.profileCommonName ?? "None"} />
          <Field label="Acquired" value={formatDate(plant.acquiredDate)} />
          <Field label="Last watered" value={formatInstant(plant.lastWateredAt, false)} />
          <Field label="Watering interval" value={plant.wateringIntervalDays ? `${plant.wateringIntervalDays} days` : "Not scheduled"} />
          <Field label="Next due" value={formatDate(plant.nextDueDate)} />
        </CardContent>
      </Card>

      {plant.careTips ? <CareTipsCard tips={plant.careTips} /> : null}

      <Card>
        <CardHeader>
          <CardTitle>Watering history</CardTitle>
        </CardHeader>
        <CardContent>
          {wateringLogs && wateringLogs.length > 0 ? (
            <ul className="space-y-1 text-sm">
              {wateringLogs.map((log) => (
                <li key={log.id} className="flex items-baseline gap-2">
                  <span className="font-medium">{formatInstant(log.wateredAt, true)}</span>
                  {log.note ? <span className="text-muted-foreground">{log.note}</span> : null}
                </li>
              ))}
            </ul>
          ) : (
            <EmptyState
              icon={Droplets}
              title="No waterings logged yet"
              description="Water this plant and the log will show up here."
            />
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
        <Button onClick={() => waterPlant.mutate({ id: plant.id })} disabled={waterPlant.isPending} className={touchButton}>
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
