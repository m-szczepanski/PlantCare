import { useNavigate, useParams } from "react-router-dom";
import { ApiError } from "@/api/client";
import type { PlantInput } from "@/api/types";
import { PlantForm } from "@/components/PlantForm";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useCreatePlant, usePlant, useUpdatePlant } from "@/hooks/usePlants";

export function PlantFormPage() {
  const { id } = useParams();
  const isEdit = id !== undefined;
  const plantId = Number(id);
  const navigate = useNavigate();

  return (
    <div className="mx-auto max-w-xl space-y-4">
      <h1 className="text-2xl font-bold">{isEdit ? "Edit plant" : "Add plant"}</h1>
      <Card>
        <CardHeader>
          <CardTitle>{isEdit ? "Update details" : "New plant"}</CardTitle>
        </CardHeader>
        <CardContent>
          {isEdit ? (
            <EditPlant plantId={plantId} onDone={() => navigate(`/plants/${plantId}`)} />
          ) : (
            <CreatePlant onDone={() => navigate("/plants")} />
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function CreatePlant({ onDone }: { onDone: () => void }) {
  const create = useCreatePlant();

  function handleSubmit(input: PlantInput) {
    create.mutate(input, { onSuccess: onDone });
  }

  return (
    <PlantForm
      submitting={create.isPending}
      error={errorMessage(create.error)}
      submitLabel="Create plant"
      onSubmit={handleSubmit}
      onCancel={onDone}
    />
  );
}

function EditPlant({ plantId, onDone }: { plantId: number; onDone: () => void }) {
  const { data: plant, isPending } = usePlant(plantId);
  const update = useUpdatePlant();

  if (isPending || !plant) {
    return <p className="text-muted-foreground">Loading plant...</p>;
  }

  function handleSubmit(input: PlantInput) {
    update.mutate({ id: plantId, input }, { onSuccess: onDone });
  }

  return (
    <PlantForm
      initial={plant}
      submitting={update.isPending}
      error={errorMessage(update.error)}
      submitLabel="Save changes"
      onSubmit={handleSubmit}
      onCancel={onDone}
    />
  );
}

function errorMessage(error: unknown): string | null {
  if (!error) return null;
  if (error instanceof ApiError && error.detail) return error.detail;
  return (error as Error).message;
}

export default PlantFormPage;
