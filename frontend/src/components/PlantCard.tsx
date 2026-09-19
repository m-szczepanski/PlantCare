import { Link } from "react-router-dom";
import { DueCount, DueStatusBadge } from "@/components/DueStatusBadge";
import { PlantPhoto } from "@/components/PlantPhoto";
import { Card, CardContent } from "@/components/ui/card";
import { wateredRelative } from "@/lib/dates";
import type { Plant } from "@/api/types";

export function PlantCard({ plant }: { plant: Plant }) {
  return (
    <Card className="transition-shadow hover:shadow-md">
      <CardContent className="flex items-start gap-4 p-6">
        <PlantPhoto
          photoUrl={plant.photoUrl}
          nickName={plant.nickName}
          className="h-14 w-14 shrink-0"
        />
        <div className="min-w-0 flex-1">
          <Link to={`/plants/${plant.id}`} className="text-lg font-semibold break-words hover:underline">
            {plant.nickName}
          </Link>
          <p className="mt-1 text-sm text-muted-foreground">{plant.location}</p>
          {plant.profileCommonName ? (
            <p className="text-sm text-muted-foreground">{plant.profileCommonName}</p>
          ) : null}
          <div className="mt-3 flex flex-wrap items-center gap-x-2 gap-y-1">
            <DueStatusBadge plant={plant} />
            <span className="text-xs text-muted-foreground">{wateredRelative(plant.lastWateredAt)}</span>
          </div>
        </div>
        <DueCount plant={plant} />
      </CardContent>
    </Card>
  );
}
