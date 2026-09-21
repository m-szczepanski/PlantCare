import { describe, expect, it } from "vitest";
import type { Plant } from "@/api/types";
import { dateKey, projectOccurrences } from "@/lib/scheduleProjection";

function plant(over: Partial<Plant>): Plant {
  return {
    id: 1,
    nickName: "Monstera Mike",
    roomId: null,

    roomName: "Living room",
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
    nextDueDate: "2026-09-22T00:00:00",
    dueMessage: "",
    roomLightMatch: null,
    ...over,
  };
}

describe("projectOccurrences", () => {
  it("projects repeating due dates inside the window", () => {
    const buckets = projectOccurrences(
      [plant({})],
      new Date(2026, 8, 19),
      new Date(2026, 9, 31),
    );

    expect(buckets.get("2026-09-22")?.map((p) => p.nickName)).toEqual(["Monstera Mike"]);
    expect(buckets.has("2026-09-29")).toBe(true);
    expect(buckets.has("2026-10-06")).toBe(true);
    expect(buckets.has("2026-10-13")).toBe(true);
    expect(buckets.has("2026-10-20")).toBe(true);
    expect(buckets.has("2026-10-27")).toBe(true);
  });

  it("places overdue plants on the first day of the window", () => {
    const buckets = projectOccurrences(
      [plant({ nextDueDate: "2026-09-15T00:00:00", dueStatus: "Overdue" })],
      new Date(2026, 8, 19),
      new Date(2026, 8, 25),
    );

    expect(buckets.get(dateKey(new Date(2026, 8, 19)))).toHaveLength(1);
  });

  it("groups several plants on shared days", () => {
    const buckets = projectOccurrences(
      [plant({ id: 1, nickName: "A" }), plant({ id: 2, nickName: "B" })],
      new Date(2026, 8, 19),
      new Date(2026, 8, 25),
    );

    expect(buckets.get("2026-09-22")).toHaveLength(2);
  });

  it("skips plants without a schedule", () => {
    const buckets = projectOccurrences(
      [
        plant({ id: 1, nextDueDate: null, wateringIntervalDays: 0, dueStatus: "NotScheduled" }),
        plant({ id: 2, wateringIntervalDays: null, nextDueDate: "2026-09-22T00:00:00" }),
      ],
      new Date(2026, 8, 19),
      new Date(2026, 9, 30),
    );

    expect(buckets.size).toBe(0);
  });
});
