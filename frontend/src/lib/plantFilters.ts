import { parseInstant } from "@/lib/dates";
import type { Plant, PlantDueStatus } from "@/api/types";

export type DueFilter = "all" | PlantDueStatus;

export type SortKey = "name" | "room" | "soonestDue" | "recentlyWatered";

export interface PlantQuery {
  search: string;
  due: DueFilter;
  sort: SortKey;
}

const DUE_FILTERS: { value: DueFilter; label: string }[] = [
  { value: "all", label: "All statuses" },
  { value: "Overdue", label: "Overdue" },
  { value: "DueToday", label: "Due today" },
  { value: "Upcoming", label: "Upcoming" },
  { value: "NotScheduled", label: "Not scheduled" },
];

const SORT_OPTIONS: { value: SortKey; label: string }[] = [
  { value: "name", label: "Name A–Z" },
  { value: "room", label: "Room" },
  { value: "soonestDue", label: "Soonest due" },
  { value: "recentlyWatered", label: "Recently watered" },
];

export const dueFilterOptions = DUE_FILTERS;
export const sortOptions = SORT_OPTIONS;

function matchesSearch(plant: Plant, query: string): boolean {
  if (query === "") return true;
  const haystack = [plant.nickName, plant.roomName ?? "", plant.profileCommonName ?? ""]
    .join(" ")
    .toLowerCase();
  return haystack.includes(query);
}

function dueValue(plant: Plant): number {
  return plant.daysUntilDue ?? Number.POSITIVE_INFINITY;
}

function wateredValue(plant: Plant): number {
  if (!plant.lastWateredAt) return 0;
  return parseInstant(plant.lastWateredAt).getTime();
}

export function filterPlants(plants: Plant[], query: PlantQuery): Plant[] {
  const search = query.search.trim().toLowerCase();

  const matched = plants.filter(
    (plant) => matchesSearch(plant, search) && (query.due === "all" || plant.dueStatus === query.due),
  );

  const sorted = [...matched];
  switch (query.sort) {
    case "name":
      sorted.sort((a, b) => a.nickName.localeCompare(b.nickName));
      break;
    case "room":
      sorted.sort((a, b) => (a.roomName ?? "\uffff").localeCompare(b.roomName ?? "\uffff") || a.nickName.localeCompare(b.nickName));
      break;
    case "soonestDue":
      sorted.sort((a, b) => dueValue(a) - dueValue(b));
      break;
    case "recentlyWatered":
      sorted.sort((a, b) => wateredValue(b) - wateredValue(a));
      break;
  }
  return sorted;
}
