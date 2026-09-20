import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { DashboardStatsStrip } from "@/components/DashboardStatsStrip";
import { NoPlantsEmptyState } from "@/components/NoPlantsEmptyState";
import { PlantCard } from "@/components/PlantCard";
import { PlantCardSkeletonGrid } from "@/components/PlantCardSkeleton";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { useDashboard } from "@/hooks/useDashboard";
import { useSnoozeAllPlants, useWaterPlant } from "@/hooks/usePlants";
import { dashboardSections, type DashboardSection } from "@/lib/dashboard";
import type { Dashboard } from "@/api/types";
import { touchButton } from "@/lib/ui";

export function DashboardPage() {
  const { data: dashboard, isPending, isError, error } = useDashboard();
  const water = useWaterPlant();
  const snoozeAll = useSnoozeAllPlants();
  const [params, setParams] = useSearchParams();
  const group = params.get("group");
  const [vacationDays, setVacationDays] = useState("14");

  if (isPending) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <PlantCardSkeletonGrid label="Loading dashboard..." />
      </div>
    );
  }

  if (isError || !dashboard) {
    return (
      <Card>
        <CardContent className="pt-6 text-destructive">
          Could not load dashboard: {(error as Error)?.message ?? "Unknown error"}
        </CardContent>
      </Card>
    );
  }

  const totalPlants = dashboard.overdue.length + dashboard.dueToday.length + dashboard.upcoming.length;

  if (totalPlants === 0) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <NoPlantsEmptyState />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <div className="flex items-center gap-2">
          <select
            aria-label="Vacation snooze days"
            value={vacationDays}
            onChange={(event) => setVacationDays(event.target.value)}
            className="h-9 rounded-md border border-input bg-transparent px-2 text-sm"
          >
            {[3, 7, 14, 30, 60].map((days) => (
              <option key={days} value={String(days)}>{days} days</option>
            ))}
          </select>
          <Button
            variant="outline"
            className={touchButton}
            disabled={snoozeAll.isPending}
            onClick={() => snoozeAll.mutate(Number(vacationDays))}
          >
            Snooze all
          </Button>
          <Button asChild className={touchButton}>
            <Link to="/plants/new">Add plant</Link>
          </Button>
        </div>
      </div>

      <DashboardStatsStrip dashboard={dashboard} />

      <div className="flex gap-2" role="group" aria-label="Dashboard grouping">
        <Button
          size="sm"
          variant={group === "room" ? "outline" : "default"}
          onClick={() => setParams(new URLSearchParams(), { replace: true })}
        >
          By due date
        </Button>
        <Button
          size="sm"
          variant={group === "room" ? "default" : "outline"}
          onClick={() => setParams(new URLSearchParams({ group: "room" }), { replace: true })}
        >
          By room
        </Button>
      </div>

      {(group === "room" ? roomSections(dashboard) : dashboardSections).map((section) => {
        const plants = section.select(dashboard);
        if (plants.length === 0) return null;
        return (
          <section key={section.key} className="space-y-3">
            <h2 className="text-lg font-semibold">{section.title}</h2>
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

function roomSections(dashboard: Dashboard): DashboardSection[] {
  const all = [...dashboard.overdue, ...dashboard.dueToday, ...dashboard.upcoming];
  const names = [...new Set(all.map((plant) => plant.roomName ?? "No room"))].sort((a, b) =>
    a.localeCompare(b),
  );
  return names.map((name) => ({
    key: `room:${name}`,
    title: name,
    select: (d: Dashboard) =>
      [...d.overdue, ...d.dueToday, ...d.upcoming].filter((p) => (p.roomName ?? "No room") === name),
  }));
}

export default DashboardPage;
