import { useMemo } from "react";
import type { WateringLogEntry } from "@/api/types";

const MONTHS_SHOWN = 6;
const DAY_MS = 86_400_000;

interface MonthBucket {
  key: string;
  label: string;
  count: number;
}

function parseInstant(value: string): Date {
  return new Date(value.endsWith("Z") ? value : `${value}Z`);
}

function bucketize(logs: WateringLogEntry[]): MonthBucket[] {
  const now = new Date();
  const months: MonthBucket[] = [];
  for (let back = MONTHS_SHOWN - 1; back >= 0; back -= 1) {
    const date = new Date(now.getFullYear(), now.getMonth() - back, 1);
    months.push({
      key: `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}`,
      label: date.toLocaleDateString(undefined, { month: "short", year: "numeric" }),
      count: 0,
    });
  }

  const index = new Map(months.map((month, position) => [month.key, position]));
  for (const log of logs) {
    const date = parseInstant(log.wateredAt);
    const key = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}`;
    const position = index.get(key);
    if (position !== undefined) {
      months[position].count += 1;
    }
  }

  return months;
}

export function WateringHistoryChart({ logs }: { logs: WateringLogEntry[] }) {
  const months = useMemo(() => bucketize(logs), [logs]);
  const total = months.reduce((sum, month) => sum + month.count, 0);
  const max = Math.max(...months.map((month) => month.count), 1);

  if (total === 0) {
    return null;
  }

  return (
    <figure className="space-y-2" aria-label={`Waterings per month over the last ${MONTHS_SHOWN} months`}>
      <div className="flex h-28 items-end gap-2" aria-hidden="true">
        {months.map((month) => (
          <div key={month.key} className="flex flex-1 flex-col items-center justify-end gap-1">
            <span className="text-xs font-medium text-muted-foreground">{month.count}</span>
            <div
              className={
                month.count === 0
                  ? "h-px w-full rounded bg-border"
                  : "w-full rounded bg-chart-1 transition-[height]"
              }
              style={{ height: month.count === 0 ? 1 : `${Math.max(8, (month.count / max) * 100)}%` }}
              title={`${month.label}: ${month.count} waterings`}
            />
            <span className="text-xs text-muted-foreground">{month.label.split(" ")[0]}</span>
          </div>
        ))}
      </div>
      <figcaption className="sr-only">
        <table>
          <thead>
            <tr>
              <th scope="col">Month</th>
              <th scope="col">Waterings</th>
            </tr>
          </thead>
          <tbody>
            {months.map((month) => (
              <tr key={month.key}>
                <td>{month.label}</td>
                <td>{month.count}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </figcaption>
    </figure>
  );
}
