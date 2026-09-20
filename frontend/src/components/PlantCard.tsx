import { Link } from "react-router-dom";
import { AlertTriangle, Droplets } from "lucide-react";
import { DueCount, DueStatusBadge } from "@/components/DueStatusBadge";
import { PlantPhoto } from "@/components/PlantPhoto";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { wateredRelative } from "@/lib/dates";
import { touchButton } from "@/lib/ui";
import type { Plant } from "@/api/types";

interface PlantCardProps {
  plant: Plant;
  onWater?: (plant: Plant) => void;
  isWatering?: boolean;
}

export function PlantCard({ plant, onWater, isWatering }: PlantCardProps) {
  return (
    <Card
      data-plant-card={plant.id}
      tabIndex={0}
      className="transition-shadow hover:shadow-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
    >
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
          <p className="mt-1 text-sm text-muted-foreground">{plant.roomName ?? "No room"}</p>
          {plant.profileCommonName ? (
            <p className="flex items-center gap-1 text-sm text-muted-foreground">
              {plant.profileCommonName}
              {plant.profileToxicToPets || plant.profileToxicToChildren ? (
                <span
                  role="img"
                  aria-label={
                    [
                      plant.profileToxicToPets ? "toxic to pets" : null,
                      plant.profileToxicToChildren ? "toxic to children" : null,
                    ]
                      .filter(Boolean)
                      .join(" and ")
                  }
                >
                  <AlertTriangle className="h-3.5 w-3.5 text-destructive" aria-hidden="true" />
                </span>
              ) : null}
            </p>
          ) : null}
          <div className="mt-3 flex flex-wrap items-center gap-x-2 gap-y-1">
            <DueStatusBadge plant={plant} />
            <span className="text-xs text-muted-foreground">{wateredRelative(plant.lastWateredAt)}</span>
            {onWater ? (
              <Button
                size="sm"
                variant="outline"
                data-water-button
                title="Water this plant (w)"
                className={`${touchButton} ml-auto`}
                disabled={isWatering}
                onClick={() => onWater(plant)}
              >
                <Droplets className="h-4 w-4" aria-hidden="true" />
                {isWatering ? "Watering..." : "Water"}
              </Button>
            ) : null}
          </div>
        </div>
        <DueCount plant={plant} />
      </CardContent>
    </Card>
  );
}
