import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { AlertTriangle, CalendarClock, Compass, Droplets, Stethoscope } from "lucide-react";
import { DueCount, DueStatusBadge } from "@/components/DueStatusBadge";
import { HealthBadge } from "@/components/HealthBadge";
import { PlantPhoto } from "@/components/PlantPhoto";
import { RoomLightBadge } from "@/components/RoomLightBadge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { formatDate, wateredRelative } from "@/lib/dates";
import { touchButton } from "@/lib/ui";
import type { Plant } from "@/api/types";

interface PlantCardProps {
  plant: Plant;
  onWater?: (plant: Plant) => void;
  isWatering?: boolean;
}

export function PlantCard({ plant, onWater, isWatering }: PlantCardProps) {
  const { t, i18n } = useTranslation();
  const soilWetActive =
    !!plant.soilWetUntil && new Date(plant.soilWetUntil).getTime() > Date.now();
  return (
    <Card
      data-plant-card={plant.id}
      tabIndex={0}
      className="flex h-full flex-col overflow-hidden transition-shadow hover:shadow-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
    >
      <PlantPhoto
        photoUrl={plant.photoUrl}
        nickName={plant.nickName}
        className="aspect-[16/7] w-full shrink-0 rounded-none"
      />
      <CardContent className="flex flex-1 flex-col gap-3 p-6">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <Link to={`/plants/${plant.id}`} className="text-xl font-semibold break-words hover:underline">
              {plant.nickName}
            </Link>
            <p className="mt-0.5 flex flex-wrap items-center gap-1.5 text-sm text-muted-foreground">
              <span className="inline-flex min-w-0 items-center gap-1">
                <Compass className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
                <span className="break-words">{plant.roomName ?? t("plant.noRoom")}</span>
              </span>
              {plant.roomLightMatch ? <RoomLightBadge match={plant.roomLightMatch} /> : null}
            </p>
            <p className="mt-0.5 flex items-center gap-1 text-sm text-muted-foreground">
              <span className="break-words">{plant.profileCommonName ?? t("plant.noProfile")}</span>
              {plant.profileToxicToPets || plant.profileToxicToChildren ? (
                <span
                  role="img"
                  aria-label={[
                    plant.profileToxicToPets && t("plant.toxicToPets"),
                    plant.profileToxicToChildren && t("plant.toxicToChildren"),
                  ]
                    .filter(Boolean)
                    .join(` ${t("common.and")} `)}
                >
                  <AlertTriangle className="h-3.5 w-3.5 text-destructive" aria-hidden="true" />
                </span>
              ) : null}
            </p>
          </div>
          <DueCount plant={plant} />
        </div>

        <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
          <DueStatusBadge plant={plant} />
          {plant.healthStatus ? <HealthBadge status={plant.healthStatus} /> : null}
          {plant.checkupDue ? (
            <span className="flex items-center gap-1 text-xs font-medium" role="status">
              <Stethoscope className="h-3 w-3" aria-hidden="true" />
              {t("plant.checkupDue")}
            </span>
          ) : null}
        </div>

        <div className="mt-auto space-y-1 border-t pt-3 text-sm text-muted-foreground">
          <p className="flex items-center gap-1.5">
            <Droplets className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
            {wateredRelative(plant.lastWateredAt)}
            {soilWetActive ? (
              <span className="text-xs" role="status">
                · {t("plant.soilWetUntil", {
                  date: new Date(plant.soilWetUntil as string).toLocaleDateString(i18n.language),
                })}
              </span>
            ) : null}
          </p>
          <p className="flex items-center gap-1.5">
            <CalendarClock className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
            {plant.nextDueDate
              ? t("plant.nextDueOn", { date: formatDate(plant.nextDueDate, i18n.language) })
              : plant.wateringIntervalDays !== null
                ? t("plant.wateringInterval", { interval: t("common.days", { count: plant.wateringIntervalDays }) })
                : t("plant.noSchedule")}
          </p>
        </div>

        {onWater ? (
          <Button
            size="sm"
            variant="outline"
            data-water-button
            title={t("plant.waterShortcut")}
            className={`${touchButton} mt-1 self-start`}
            disabled={isWatering}
            onClick={() => onWater(plant)}
          >
            <Droplets className="h-4 w-4" aria-hidden="true" />
            {isWatering ? t("plant.watering") : t("plant.water")}
          </Button>
        ) : null}
      </CardContent>
    </Card>
  );
}