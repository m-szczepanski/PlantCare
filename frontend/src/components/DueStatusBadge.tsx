import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { Plant } from "@/api/types";

const variantByStatus: Record<Plant["dueStatus"], "default" | "destructive" | "secondary" | "outline"> = {
  Overdue: "destructive",
  DueToday: "default",
  Upcoming: "secondary",
  NotScheduled: "outline",
};

export function DueStatusBadge({ plant, className }: { plant: Plant; className?: string }) {
  return (
    <Badge variant={variantByStatus[plant.dueStatus]} className={cn("capitalize", className)}>
      {plant.dueMessage}
    </Badge>
  );
}
