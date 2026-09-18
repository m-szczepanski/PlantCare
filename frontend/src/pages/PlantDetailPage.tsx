import { Link, useNavigate, useParams } from "react-router-dom";
import { DueStatusBadge } from "@/components/DueStatusBadge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useDeletePlant, usePlant } from "@/hooks/usePlants";

function formatDate(value: string | null): string {
  if (!value) return "-";
  return new Date(value).toLocaleDateString();
}

export function PlantDetailPage() {
  const { id } = useParams();
  const plantId = Number(id);
  const navigate = useNavigate();
  const { data: plant, isPending, isError, error } = usePlant(plantId);
  const deletePlant = useDeletePlant();

  if (isPending) {
    return <p className="text-muted-foreground">Loading plant...</p>;
  }

  if (isError || !plant) {
    return (
      <Card>
        <CardContent className="pt-6 text-destructive">
          Could not load plant: {(error as Error)?.message ?? "Not found"}
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

      <Card>
        <CardHeader>
          <CardTitle>Details</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 text-sm sm:grid-cols-2">
          <Field label="Location" value={plant.location} />
          <Field label="Species profile" value={plant.profileCommonName ?? "None"} />
          <Field label="Acquired" value={formatDate(plant.acquiredDate)} />
          <Field label="Last watered" value={formatDate(plant.lastWateredAt)} />
          <Field label="Watering interval" value={plant.wateringIntervalDays ? `${plant.wateringIntervalDays} days` : "Not scheduled"} />
          <Field label="Next due" value={formatDate(plant.nextDueDate)} />
          <Field label="Photo" value={plant.photoUrl ?? "None"} />
        </CardContent>
      </Card>

      <div className="flex gap-2">
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
