import { Link } from "react-router-dom";
import { DueStatusBadge } from "@/components/DueStatusBadge";
import { Card, CardContent } from "@/components/ui/card";
import type { Plant } from "@/api/types";

export function PlantCard({ plant }: { plant: Plant }) {
  return (
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
  );
}
