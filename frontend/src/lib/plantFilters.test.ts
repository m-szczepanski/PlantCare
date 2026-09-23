import { describe, expect, it } from "vitest";
import type { Plant } from "@/api/types";
import { filterPlants } from "@/lib/plantFilters";

function plant(over: Partial<Plant>): Plant {
  return {
    id: 1,
    nickName: "Alpha",
    roomId: null,

    roomName: "Kitchen",
    photoUrl: null,
    potSizeCm: null,
    soilType: null,
    soilMix: null,
    propagatedFrom: null,
    notifyEnabled: true,
    snoozedUntil: null,
    soilWetUntil: null,
    acquiredDate: "2026-01-01T00:00:00",
    plantProfileId: null,
    profileCommonName: null,
    profileToxicToPets: false,
    profileToxicToChildren: false,
    careTips: null,
    customWateringIntervalDays: null,
    reduceInWinter: null,
    lastWateredAt: null,
    dueStatus: "Upcoming",
    wateringIntervalDays: 7,
    daysUntilDue: 3,
    nextDueDate: null,
    dueMessage: "",
    roomLightMatch: null,
    healthStatus: null,
    lastCheckupAt: null,
    checkupDue: false,
    ...over,
  };
}

const plants = [
  plant({ id: 1, nickName: "Zoe", roomId: null,
 roomName: "Bedroom", profileCommonName: "Pothos", dueStatus: "Overdue", daysUntilDue: -2, lastWateredAt: "2026-03-01T00:00:00" }),
  plant({ id: 2, nickName: "Monstera Mike", roomId: null,
 roomName: "Living room", profileCommonName: "Monstera", dueStatus: "DueToday", daysUntilDue: 0, lastWateredAt: "2026-03-10T00:00:00" }),
  plant({ id: 3, nickName: "Bamboo", roomId: null,
 roomName: "Office", dueStatus: "Upcoming", daysUntilDue: 5, lastWateredAt: null }),
];

const query = { search: "", due: "all", sort: "name" } as const;
const names = (list: Plant[]) => list.map((p) => p.nickName);

describe("filterPlants", () => {
  it("searches across name, room and species", () => {
    expect(names(filterPlants(plants, { ...query, search: "pot" }))).toEqual(["Zoe"]);
    expect(names(filterPlants(plants, { ...query, search: "office" }))).toEqual(["Bamboo"]);
    expect(names(filterPlants(plants, { ...query, search: "MONSTERA" }))).toEqual(["Monstera Mike"]);
  });

  it("filters by due status", () => {
    expect(names(filterPlants(plants, { ...query, due: "Overdue" }))).toEqual(["Zoe"]);
    expect(names(filterPlants(plants, { ...query, due: "NotScheduled" }))).toEqual([]);
  });

  it("sorts by name and room", () => {
    expect(names(filterPlants(plants, { ...query, sort: "name" }))).toEqual(["Bamboo", "Monstera Mike", "Zoe"]);
    expect(names(filterPlants(plants, { ...query, sort: "room" }))).toEqual(["Zoe", "Monstera Mike", "Bamboo"]);
  });

  it("sorts by soonest due with unscheduled last", () => {
    const withUnscheduled = [...plants, plant({ id: 4, nickName: "Saguaro", dueStatus: "NotScheduled", daysUntilDue: null })];
    expect(names(filterPlants(withUnscheduled, { ...query, sort: "soonestDue" }))).toEqual([
      "Zoe",
      "Monstera Mike",
      "Bamboo",
      "Saguaro",
    ]);
  });

  it("sorts by most recently watered with never-watered last", () => {
    expect(names(filterPlants(plants, { ...query, sort: "recentlyWatered" }))).toEqual([
      "Monstera Mike",
      "Zoe",
      "Bamboo",
    ]);
  });

  it("does not mutate the input array", () => {
    const original = [...plants];
    filterPlants(plants, { ...query, sort: "soonestDue" });
    expect(plants).toEqual(original);
  });
});
