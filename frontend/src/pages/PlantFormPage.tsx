import { useNavigate, useParams } from "react-router-dom";
import type { PlantInput } from "@/api/types";
import { PlantForm } from "@/components/PlantForm";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { splitApiError } from "@/lib/validation";
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
  const split = splitApiError(create.error);

  function handleSubmit(input: PlantInput) {
    create.mutate(input, { onSuccess: onDone });
  }

  return (
    <PlantForm
      submitting={create.isPending}
      error={split.banner}
      fieldErrors={split.fields}
      submitLabel="Create plant"
      onSubmit={handleSubmit}
      onCancel={onDone}
    />
  );
}

function EditPlant({ plantId, onDone }: { plantId: number; onDone: () => void }) {
  const { data: plant, isPending, isError } = usePlant(plantId);
  const update = useUpdatePlant();
  const split = splitApiError(update.error);

  if (isPending) {
    return <p className="text-muted-foreground">Loading plant...</p>;
  }

  if (isError || !plant) {
    return <p className="text-destructive">Could not load plant.</p>;
  }

  function handleSubmit(input: PlantInput) {
    update.mutate({ id: plantId, input }, { onSuccess: onDone });
  }

  return (
    <PlantForm
      initial={plant}
      submitting={update.isPending}
      error={split.banner}
      fieldErrors={split.fields}
      submitLabel="Save changes"
      onSubmit={handleSubmit}
      onCancel={onDone}
    />
  );
}

export default PlantFormPage;
