import { Link } from "react-router-dom";
import { DueStatusBadge } from "@/components/DueStatusBadge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { usePlants } from "@/hooks/usePlants";

export function PlantsPage() {
  const { data: plants, isPending, isError, error } = usePlants();

  if (isPending) {
    return <p className="text-muted-foreground">Loading plants...</p>;
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
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            You have no plants yet.{" "}
            <Link to="/plants/new" className="font-medium text-primary underline">
              Add your first plant
            </Link>
            .
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Plants</h1>
        <Button asChild>
          <Link to="/plants/new">Add plant</Link>
        </Button>
      </div>

      <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {plants.map((plant) => (
          <li key={plant.id}>
            <Card className="transition-shadow hover:shadow-md">
              <CardContent className="p-6">
                <div className="flex items-start justify-between gap-2">
                  <Link to={`/plants/${plant.id}`} className="text-lg font-semibold hover:underline">
                    {plant.nickName}
                  </Link>
                  <DueStatusBadge plant={plant} />
                </div>
                <p className="mt-1 text-sm text-muted-foreground">{plant.location}</p>
                {plant.profileCommonName ? (
                  <p className="text-sm text-muted-foreground">{plant.profileCommonName}</p>
                ) : null}
              </CardContent>
            </Card>
          </li>
        ))}
      </ul>
    </div>
  );
}

export default PlantsPage;
