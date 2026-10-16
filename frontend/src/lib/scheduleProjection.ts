import type { Plant } from "@/api/types";
import { dateKey, parseDateOnly } from "@/lib/dates";

export { dateKey };

function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

/**
 * Projects each plant's repeating due dates (nextDueDate + N * interval) onto
 * the [from, to] window. Plants that are already overdue when the window opens
 * are placed on the first day of the window. Keys are local yyyy-mm-dd.
 */
export function projectOccurrences(
  plants: Plant[],
  from: Date,
  to: Date,
): Map<string, Plant[]> {
  const buckets = new Map<string, Plant[]>();

  for (const plant of plants) {
    const interval = plant.wateringIntervalDays;
    if (!plant.nextDueDate || !interval || interval < 1) continue;

    const dueDate = parseDateOnly(plant.nextDueDate);
    let cursor = dueDate < from ? from : dueDate;
    cursor = new Date(cursor.getFullYear(), cursor.getMonth(), cursor.getDate());

    while (cursor <= to) {
      const key = dateKey(cursor);
      const bucket = buckets.get(key);
      if (bucket) {
        bucket.push(plant);
      } else {
        buckets.set(key, [plant]);
      }
      cursor = addDays(cursor, interval);
    }
  }

  return buckets;
}
