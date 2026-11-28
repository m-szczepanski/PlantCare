import { CheckCircle2, Droplets, Sprout } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Card, CardContent } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import type { Dashboard } from "@/api/types";

function Stat({
  icon: Icon,
  value,
  label,
  emphasis,
}: {
  icon: typeof Droplets;
  value: number;
  label: string;
  emphasis?: "danger" | "ok";
}) {
  return (
    <div role="group" aria-label={`${label}: ${value}`} className="flex flex-1 items-center gap-3 px-5 py-4">
      <Icon
        className={cn(
          "h-6 w-6 shrink-0 text-muted-foreground",
          emphasis === "danger" && "text-destructive",
          emphasis === "ok" && "text-primary",
        )}
        aria-hidden="true"
      />
      <div>
        <div
          className={cn(
            "text-3xl font-bold leading-none",
            emphasis === "danger" && "text-destructive",
            emphasis === "ok" && "text-primary",
          )}
        >
          {value}
        </div>
        <div className="mt-0.5 text-sm text-muted-foreground">{label}</div>
      </div>
    </div>
  );
}

export function DashboardStatsStrip({ dashboard }: { dashboard: Dashboard }) {
  const { t } = useTranslation();
  const overdue = dashboard.overdue.length;
  const dueToday = dashboard.dueToday.length;
  const total = overdue + dueToday + dashboard.upcoming.length;
  const allCaughtUp = overdue === 0 && dueToday === 0;

  return (
    <Card>
      <CardContent className="p-0">
        <div className="flex flex-wrap divide-x sm:flex-nowrap">
          <Stat
            icon={Droplets}
            value={overdue}
            label={t("stats.overdue")}
            emphasis={overdue > 0 ? "danger" : "ok"}
          />
          <Stat icon={CheckCircle2} value={dueToday} label={t("stats.dueToday")} emphasis={dueToday > 0 ? undefined : "ok"} />
          <Stat icon={Sprout} value={total} label={t("stats.plants")} />
        </div>
        {allCaughtUp ? (
          <div className="border-t bg-primary/10 px-4 py-2 text-sm font-medium text-primary">
            {t("stats.allCaughtUp")}
          </div>
        ) : (
          <div className="border-t px-4 py-2 text-sm text-muted-foreground">
            {overdue > 0 ? t("stats.summaryWithOverdue", { overdue, dueToday }) : t("stats.summaryDueToday", { dueToday })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
