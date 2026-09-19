import { Link } from "react-router-dom";
import { NoPlantsEmptyState } from "@/components/NoPlantsEmptyState";
import { PlantCard } from "@/components/PlantCard";
import { PlantCardSkeletonGrid } from "@/components/PlantCardSkeleton";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { useDashboard } from "@/hooks/useDashboard";
import type { Dashboard, Plant } from "@/api/types";

const sections: { key: string; title: string; select: (d: Dashboard) => Plant[] }[] = [
  { key: "overdue", title: "Overdue", select: (d) => d.overdue },
  { key: "dueToday", title: "Due today", select: (d) => d.dueToday },
  { key: "upcoming", title: "Upcoming", select: (d) => d.upcoming },
];

export function DashboardPage() {
  const { data: dashboard, isPending, isError, error } = useDashboard();

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
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <Button asChild>
          <Link to="/plants/new">Add plant</Link>
        </Button>
      </div>

      {sections.map((section) => {
        const plants = section.select(dashboard);
        if (plants.length === 0) return null;
        return (
          <section key={section.key} className="space-y-3">
            <h2 className="text-lg font-semibold">{section.title}</h2>
            <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {plants.map((plant) => (
                <li key={plant.id}>
                  <PlantCard plant={plant} />
                </li>
              ))}
            </ul>
          </section>
        );
      })}
    </div>
  );
}

export default DashboardPage;
