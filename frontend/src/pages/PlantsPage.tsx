import { Link } from "react-router-dom";
import { NoPlantsEmptyState } from "@/components/NoPlantsEmptyState";
import { PlantCard } from "@/components/PlantCard";
import { PlantCardSkeletonGrid } from "@/components/PlantCardSkeleton";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { usePlants, useWaterPlant } from "@/hooks/usePlants";
import { touchButton } from "@/lib/ui";

export function PlantsPage() {
  const { data: plants, isPending, isError, error } = usePlants();
  const water = useWaterPlant();

  if (isPending) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-bold">Plants</h1>
        <PlantCardSkeletonGrid label="Loading plants..." />
      </div>
    );
  }

  if (isError) {
    return (
      <Card>
        <CardContent className="pt-6 text-destructive">
          Could not load plants: {(error as Error).message}
        </CardContent>
      </Card>
    );
  }

  if (!plants || plants.length === 0) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-bold">Plants</h1>
        <NoPlantsEmptyState />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Plants</h1>
        <Button asChild className={touchButton}>
          <Link to="/plants/new">Add plant</Link>
        </Button>
      </div>

      <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {plants.map((plant) => (
          <li key={plant.id}>
            <PlantCard
              plant={plant}
              onWater={(p) => water.mutate({ id: p.id })}
              isWatering={water.isPending && water.variables?.id === plant.id}
            />
          </li>
        ))}
      </ul>
    </div>
  );
}

export default PlantsPage;
