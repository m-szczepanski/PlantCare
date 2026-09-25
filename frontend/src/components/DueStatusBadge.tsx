import type { ComponentType } from "react";
import { AlertTriangle, CalendarClock, CircleDashed, Clock } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Badge } from "@/components/ui/badge";
import type { Plant, PlantDueStatus } from "@/api/types";

interface StatusMeta {
  labelKey: string;
  variant: "default" | "destructive" | "secondary" | "outline";
  icon: ComponentType<{ className?: string }>;
}

const metaByStatus: Record<PlantDueStatus, StatusMeta> = {
  Overdue: { labelKey: "due.overdue", variant: "destructive", icon: AlertTriangle },
  DueToday: { labelKey: "due.dueToday", variant: "default", icon: Clock },
  Upcoming: { labelKey: "due.upcoming", variant: "secondary", icon: CalendarClock },
  NotScheduled: { labelKey: "due.notScheduled", variant: "outline", icon: CircleDashed },
};

export function DueStatusBadge({ plant }: { plant: Plant }) {
  const { t } = useTranslation();
  const { labelKey, variant, icon: Icon } = metaByStatus[plant.dueStatus];

  return (
    <Badge variant={variant} className="gap-1">
      <Icon className="h-3 w-3" aria-hidden="true" />
      {t(labelKey)}
    </Badge>
  );
}

export function DueCount({ plant }: { plant: Plant }) {
  const { t } = useTranslation();
  const { dueStatus, daysUntilDue } = plant;

  let value: string;
  let label: string;
  if (dueStatus === "Overdue" && daysUntilDue !== null) {
    const days = Math.abs(daysUntilDue);
    value = String(days);
    label = t("due.daysOverdue", { count: days });
  } else if (dueStatus === "DueToday") {
    value = t("due.now");
    label = t("due.dueTodayLabel");
  } else if (dueStatus === "Upcoming" && daysUntilDue !== null) {
    value = String(daysUntilDue);
    label = t("due.daysToGo", { count: daysUntilDue });
  } else {
    value = "—";
    label = t("due.notScheduledLabel");
  }

  return (
    <div className="shrink-0 text-right">
      <div
        className={
          dueStatus === "Overdue"
            ? "text-3xl font-bold leading-none text-destructive"
            : "text-3xl font-bold leading-none"
        }
      >
        {value}
      </div>
      <div className="mt-0.5 text-xs text-muted-foreground">{label}</div>
    </div>
  );
}
