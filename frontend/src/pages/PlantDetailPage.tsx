import { useRef } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { AlertTriangle, Droplets } from "lucide-react";
import { CareTipsCard } from "@/components/CareTipsCard";
import { DueStatusBadge } from "@/components/DueStatusBadge";
import { EmptyState } from "@/components/EmptyState";
import { PlantDetailSkeleton } from "@/components/PlantDetailSkeleton";
import { PlantPhoto } from "@/components/PlantPhoto";
import { ApiError } from "@/api/client";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useDeletePlant, usePlant, useUploadPlantPhoto, useWaterPlant, useWateringLogs } from "@/hooks/usePlants";

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
    if (!window.confirm(`Delete ${plant!.nickName}?`)) return;
    await deletePlant.mutateAsync(plant!.id);
    navigate("/plants");
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">{plant.nickName}</h1>
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
        <Button onClick={() => waterPlant.mutate({ id: plant.id })} disabled={waterPlant.isPending}>
          {waterPlant.isPending ? "Watering..." : "Mark as watered"}
        </Button>
        <Button variant="secondary" onClick={() => photoInputRef.current?.click()} disabled={uploadPhoto.isPending}>
          {uploadPhoto.isPending ? "Uploading..." : plant.photoUrl ? "Change photo" : "Upload photo"}
        </Button>
        <Button asChild>
          <Link to={`/plants/${plant.id}/edit`}>Edit</Link>
        </Button>
        <Button variant="outline" asChild>
          <Link to="/plants">Back to list</Link>
        </Button>
        <Button variant="destructive" onClick={handleDelete} disabled={deletePlant.isPending}>
          {deletePlant.isPending ? "Deleting..." : "Delete"}
        </Button>
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
