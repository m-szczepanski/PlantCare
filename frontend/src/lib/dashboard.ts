import type { Dashboard, Plant } from "@/api/types";

export interface DashboardSection {
  key: string;
  titleKey?: string;
  title?: string;
  select: (d: Dashboard) => Plant[];
}

export const dashboardSections: DashboardSection[] = [
  { key: "overdue", titleKey: "due.overdue", select: (d) => d.overdue },
  { key: "dueToday", titleKey: "due.dueToday", select: (d) => d.dueToday },
  { key: "upcoming", titleKey: "due.upcoming", select: (d) => d.upcoming },
];
