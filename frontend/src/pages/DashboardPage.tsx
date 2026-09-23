import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useSearchParams } from "react-router-dom";
import { CircleCheck, CircleDashed } from "lucide-react";
import { DashboardStatsStrip } from "@/components/DashboardStatsStrip";
import { CheckupBanner } from "@/components/CheckupBanner";
import { NoPlantsEmptyState } from "@/components/NoPlantsEmptyState";
import { PlantCard } from "@/components/PlantCard";
import { PlantCardSkeletonGrid } from "@/components/PlantCardSkeleton";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useDashboard } from "@/hooks/useDashboard";
import { useStatus } from "@/hooks/useStatus";
import { useBulkWater, useSnoozeAllPlants, useWaterPlant } from "@/hooks/usePlants";
import { dashboardSections, type DashboardSection } from "@/lib/dashboard";
import type { Dashboard } from "@/api/types";
import { touchButton } from "@/lib/ui";

export function DashboardPage() {
  const { t, i18n } = useTranslation();
  const { data: dashboard, isPending, isError, error } = useDashboard();
  const water = useWaterPlant();
  const snoozeAll = useSnoozeAllPlants();
  const bulkWater = useBulkWater();
  const [params, setParams] = useSearchParams();
  const group = params.get("group");
  const [vacationDays, setVacationDays] = useState("14");

  if (isPending) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">{t("dashboard.title")}</h1>
        <PlantCardSkeletonGrid label={t("dashboard.loading")} />
      </div>
    );
  }

  if (isError || !dashboard) {
    return (
      <Card>
        <CardContent className="pt-6 text-destructive">
          {t("dashboard.loadError", { message: (error as Error)?.message ?? t("common.unknownError") })}
        </CardContent>
      </Card>
    );
  }

  const totalPlants = dashboard.overdue.length + dashboard.dueToday.length + dashboard.upcoming.length;

  if (totalPlants === 0) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-bold">{t("dashboard.title")}</h1>
        <NoPlantsEmptyState />
        <OnboardingStep />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">{t("dashboard.title")}</h1>
        <div className="flex items-center gap-2">
          <select
            aria-label={t("dashboard.snoozeDaysAria")}
            value={vacationDays}
            onChange={(event) => setVacationDays(event.target.value)}
            className="h-9 rounded-md border border-input bg-transparent px-2 text-sm"
          >
            {[3, 7, 14, 30, 60].map((days) => (
              <option key={days} value={String(days)}>{t("common.days", { count: days })}</option>
            ))}
          </select>
          <Button
            variant="outline"
            className={touchButton}
            disabled={snoozeAll.isPending}
            onClick={() => snoozeAll.mutate(Number(vacationDays))}
          >
            {t("dashboard.snoozeAll")}
          </Button>
          <Button asChild className={touchButton}>
            <Link to="/plants/new">{t("dashboard.addPlant")}</Link>
          </Button>
        </div>
      </div>

      <DashboardStatsStrip dashboard={dashboard} />

      <CheckupBanner
        plants={[...dashboard.overdue, ...dashboard.dueToday, ...dashboard.upcoming].filter((p) => p.checkupDue)}
      />

      <div className="flex gap-2" role="group" aria-label={t("dashboard.groupingAria")}>
        <Button
          size="sm"
          variant={group === "room" ? "outline" : "default"}
          onClick={() => setParams(new URLSearchParams(), { replace: true })}
        >
          {t("dashboard.byDueDate")}
        </Button>
        <Button
          size="sm"
          variant={group === "room" ? "default" : "outline"}
          onClick={() => setParams(new URLSearchParams({ group: "room" }), { replace: true })}
        >
          {t("dashboard.byRoom")}
        </Button>
      </div>

      {(group === "room" ? roomSections(dashboard, t("plant.noRoom"), i18n.language) : dashboardSections).map((section) => {
        const plants = section.select(dashboard);
        if (plants.length === 0) return null;
        return (
          <section key={section.key} className="space-y-3">
            <div className="flex items-center justify-between gap-2">
              <h2 className="text-lg font-semibold">{section.titleKey ? t(section.titleKey) : section.title}</h2>
              {section.key !== "upcoming" ? (
                <Button
                  size="sm"
                  variant="outline"
                  disabled={bulkWater.isPending}
                  onClick={() => bulkWater.mutate(plants.map((p) => p.id))}
                >
                  {t("dashboard.waterAll")}
                </Button>
              ) : null}
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
          </section>
        );
      })}
    </div>
  );
}

function roomSections(dashboard: Dashboard, noRoomLabel: string, locale: string): DashboardSection[] {
  const all = [...dashboard.overdue, ...dashboard.dueToday, ...dashboard.upcoming];
  const names = [...new Set(all.map((plant) => plant.roomName ?? noRoomLabel))].sort((a, b) =>
    a.localeCompare(b, locale),
  );
  return names.map((name) => ({
    key: `room:${name}`,
    title: name,
    select: (d: Dashboard) =>
      [...d.overdue, ...d.dueToday, ...d.upcoming].filter((p) => (p.roomName ?? noRoomLabel) === name),
  }));
}

export default DashboardPage;

function OnboardingStep() {
  const { t } = useTranslation();
  const { data: status } = useStatus();

  if (!status) {
    return null;
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("onboarding.title")}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-2 text-sm">
        <p className="flex items-center gap-2">
          <CircleDashed className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
          {t("onboarding.addFirst")}
        </p>
        <p className="flex items-center gap-2">
          {status.ntfy.reachable ? (
            <CircleCheck className="h-4 w-4 text-primary" aria-hidden="true" />
          ) : (
            <CircleDashed className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
          )}
          {status.ntfy.reachable ? (
            <span>
              {t("onboarding.subscribePrefix")}{" "}
              <a
                href={status.ntfy.subscribeUrl}
                target="_blank"
                rel="noreferrer"
                className="font-medium text-primary underline"
              >
                {status.ntfy.topic}
              </a>{" "}
              {t("onboarding.subscribeSuffix")}
            </span>
          ) : (
            <span className="text-muted-foreground">
              {t("onboarding.serverUnreachable", { url: status.ntfy.baseUrl })}
            </span>
          )}
        </p>
      </CardContent>
    </Card>
  );
}
