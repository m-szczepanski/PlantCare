import { useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import type { PlantInput } from "@/api/types";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { PlantForm } from "@/components/PlantForm";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { splitApiError } from "@/lib/validation";
import { useCreatePlant, usePlant, useUpdatePlant } from "@/hooks/usePlants";

export function PlantFormPage() {
  const { t } = useTranslation();
  const { id } = useParams();
  const isEdit = id !== undefined;
  const plantId = Number(id);
  const navigate = useNavigate();

  return (
    <div className="mx-auto max-w-xl space-y-4">
      <Breadcrumbs
        items={[
          { label: t("common.home"), to: "/" },
          { label: t("plants.title"), to: "/plants" },
          { label: isEdit ? t("form.editPlant") : t("form.addPlant") },
        ]}
      />
      <h1 className="text-2xl font-bold">{isEdit ? t("form.editPlant") : t("form.addPlant")}</h1>
      <Card>
        <CardHeader>
          <CardTitle>{isEdit ? t("form.updateDetails") : t("form.newPlant")}</CardTitle>
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
  const { t } = useTranslation();
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
      submitLabel={t("form.createPlant")}
      onSubmit={handleSubmit}
      onCancel={onDone}
    />
  );
}

function EditPlant({ plantId, onDone }: { plantId: number; onDone: () => void }) {
  const { t } = useTranslation();
  const { data: plant, isPending, isError } = usePlant(plantId);
  const update = useUpdatePlant();
  const split = splitApiError(update.error);

  if (isPending) {
    return <p className="text-muted-foreground">{t("common.loadingPlant")}</p>;
  }

  if (isError || !plant) {
    return <p className="text-destructive">{t("form.loadFailed")}</p>;
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
      submitLabel={t("profiles.saveChanges")}
      onSubmit={handleSubmit}
      onCancel={onDone}
    />
  );
}

export default PlantFormPage;
