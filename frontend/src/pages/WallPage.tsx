import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Leaf } from "lucide-react";
import { DashboardStatsStrip } from "@/components/DashboardStatsStrip";
import { PlantCard } from "@/components/PlantCard";
import { PlantCardSkeletonGrid } from "@/components/PlantCardSkeleton";
import { Card, CardContent } from "@/components/ui/card";
import { useDashboard } from "@/hooks/useDashboard";
import { dashboardSections } from "@/lib/dashboard";

const REFRESH_MS = 60_000;

// Read-only ambient view for a wall tablet: no nav chrome, auto-refreshing.
export function WallPage() {
  const { t, i18n } = useTranslation();
  const { data: dashboard, isPending, isError, error } = useDashboard(REFRESH_MS);
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), 30_000);
    return () => clearInterval(timer);
  }, []);

  return (
    <div className="min-h-screen space-y-6 p-6">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2 text-3xl font-bold">
          <Leaf className="h-7 w-7 text-primary" aria-hidden="true" />
          PlantCare
        </div>
        <time
          className="text-2xl tabular-nums text-muted-foreground"
          dateTime={now.toISOString()}
        >
          {now.toLocaleDateString(i18n.language, { weekday: "short", month: "short", day: "numeric" })}{" "}
          {now.toLocaleTimeString(i18n.language, { hour: "2-digit", minute: "2-digit" })}
        </time>
      </header>

      {isPending ? (
        <PlantCardSkeletonGrid count={6} label={t("dashboard.loading")} />
      ) : isError || !dashboard ? (
        <Card>
          <CardContent className="pt-6 text-destructive">
            {t("dashboard.loadError", { message: (error as Error)?.message ?? t("common.unknownError") })}
          </CardContent>
        </Card>
      ) : (
        <>
          <DashboardStatsStrip dashboard={dashboard} />
          {dashboardSections.map((section) => {
            const plants = section.select(dashboard);
            if (plants.length === 0) return null;
            return (
              <section key={section.key} className="space-y-3">
                <h2 className="text-lg font-semibold">{section.titleKey ? t(section.titleKey) : section.title}</h2>
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
        </>
      )}
    </div>
  );
}

export default WallPage;
