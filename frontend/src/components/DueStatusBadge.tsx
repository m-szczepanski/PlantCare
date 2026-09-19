import type { ComponentType } from "react";
import { AlertTriangle, CalendarClock, CircleDashed, Clock } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import type { Plant, PlantDueStatus } from "@/api/types";

interface StatusMeta {
  label: string;
  variant: "default" | "destructive" | "secondary" | "outline";
  icon: ComponentType<{ className?: string }>;
}

const metaByStatus: Record<PlantDueStatus, StatusMeta> = {
  Overdue: { label: "Overdue", variant: "destructive", icon: AlertTriangle },
  DueToday: { label: "Due today", variant: "default", icon: Clock },
  Upcoming: { label: "Upcoming", variant: "secondary", icon: CalendarClock },
  NotScheduled: { label: "No schedule", variant: "outline", icon: CircleDashed },
};

export function DueStatusBadge({ plant }: { plant: Plant }) {
  const { label, variant, icon: Icon } = metaByStatus[plant.dueStatus];

  return (
    <Badge variant={variant} className="gap-1">
      <Icon className="h-3 w-3" aria-hidden="true" />
      {label}
    </Badge>
  );
}

export function DueCount({ plant }: { plant: Plant }) {
  const { dueStatus, daysUntilDue } = plant;

  let value: string;
  let label: string;
  if (dueStatus === "Overdue" && daysUntilDue !== null) {
    const days = Math.abs(daysUntilDue);
    value = String(days);
    label = days === 1 ? "day overdue" : "days overdue";
  } else if (dueStatus === "DueToday") {
    value = "Now";
    label = "due today";
  } else if (dueStatus === "Upcoming" && daysUntilDue !== null) {
    value = String(daysUntilDue);
    label = daysUntilDue === 1 ? "day to go" : "days to go";
  } else {
    value = "—";
    label = "not scheduled";
  }

  return (
    <div className="shrink-0 text-right">
      <div
        className={
          dueStatus === "Overdue"
            ? "text-2xl font-bold leading-none text-destructive"
            : "text-2xl font-bold leading-none"
        }
      >
        {value}
      </div>
      <div className="text-xs text-muted-foreground">{label}</div>
    </div>
  );
}
