import type { Dashboard, Plant } from "@/api/types";

export interface DashboardSection {
  key: string;
  title: string;
  select: (d: Dashboard) => Plant[];
}

export const dashboardSections: DashboardSection[] = [
  { key: "overdue", title: "Overdue", select: (d) => d.overdue },
  { key: "dueToday", title: "Due today", select: (d) => d.dueToday },
  { key: "upcoming", title: "Upcoming", select: (d) => d.upcoming },
];
