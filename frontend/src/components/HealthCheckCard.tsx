import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Stethoscope } from "lucide-react";
import { HealthBadge } from "@/components/HealthBadge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAddHealthCheck, useHealthChecks } from "@/hooks/usePlants";
import type { HealthStatus, Plant } from "@/api/types";
import { HEALTH_STATUSES } from "@/lib/health";
import { formatInstant } from "@/lib/dates";
import { touchButton, touchField } from "@/lib/ui";

export function HealthCheckCard({ plant }: { plant: Plant }) {
  const { t } = useTranslation();
  const { data: history } = useHealthChecks(plant.id);
  const addCheckup = useAddHealthCheck(plant.id);
  const [note, setNote] = useState("");

  function answer(status: HealthStatus) {
    const trimmed = note.trim();
    addCheckup.mutate(
      { status, ...(trimmed !== "" ? { note: trimmed } : {}) },
      { onSuccess: () => setNote("") },
    );
  }

  const pastChecks = history ? history.slice(1) : [];

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Stethoscope className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
          {t("health.title")}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <p className="text-sm">
          {plant.healthStatus && plant.lastCheckupAt ? (
            <span className="flex flex-wrap items-center gap-2">
              <HealthBadge status={plant.healthStatus} />
              <span className="text-muted-foreground">
                {t("health.lastCheckup", { date: formatInstant(plant.lastCheckupAt, false) })}
              </span>
            </span>
          ) : (
            <span className="text-muted-foreground">{t("health.none")}</span>
          )}
        </p>
        {plant.checkupDue ? (
          <p role="status" className="text-sm font-medium">
            {t("health.due")}
          </p>
        ) : null}
        <div className="space-y-1">
          <Label htmlFor="checkupNote">{t("health.noteOptional")}</Label>
          <Input
            id="checkupNote"
            value={note}
            onChange={(event) => setNote(event.target.value)}
            placeholder={t("health.notePlaceholder")}
            className={touchField}
          />
        </div>
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
          {HEALTH_STATUSES.map((status) => (
            <Button
              key={status}
              type="button"
              variant="outline"
              className={touchButton}
              disabled={addCheckup.isPending}
              onClick={() => answer(status)}
            >
              {t(`health.status.${status}`)}
            </Button>
          ))}
        </div>
        <p className="text-xs text-muted-foreground">{t("health.hint")}</p>
        {pastChecks.length > 0 ? (
          <div className="space-y-2 border-t pt-3">
            <p className="text-sm font-medium">{t("health.historyTitle")}</p>
            <ul className="space-y-2 text-sm">
              {pastChecks.map((entry) => (
                <li key={entry.id} className="flex flex-wrap items-baseline gap-2">
                  <HealthBadge status={entry.status} />
                  <span className="text-xs text-muted-foreground">{formatInstant(entry.checkedAt, false)}</span>
                  {entry.note ? <span className="text-muted-foreground">{entry.note}</span> : null}
                </li>
              ))}
            </ul>
          </div>
        ) : null}
      </CardContent>
    </Card>
  );
}
