import { Badge } from "@/components/ui/badge";
import type { Plant } from "@/api/types";

const variantByStatus: Record<Plant["dueStatus"], "default" | "destructive" | "secondary" | "outline"> = {
  Overdue: "destructive",
  DueToday: "default",
  Upcoming: "secondary",
  NotScheduled: "outline",
};

export function DueStatusBadge({ plant }: { plant: Plant }) {
  return <Badge variant={variantByStatus[plant.dueStatus]} className="capitalize">{plant.dueMessage}</Badge>;
}
