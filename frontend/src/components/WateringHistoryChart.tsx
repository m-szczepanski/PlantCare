import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { MonthlyBarChart, type MonthlyBarDatum } from "@/components/MonthlyBarChart";
import { parseInstant } from "@/lib/dates";
import type { WateringLogEntry } from "@/api/types";

const MONTHS_SHOWN = 6;

function bucketize(logs: WateringLogEntry[], locale: string): MonthlyBarDatum[] {
  const now = new Date();
  const months: MonthlyBarDatum[] = [];
  for (let back = MONTHS_SHOWN - 1; back >= 0; back -= 1) {
    const date = new Date(now.getFullYear(), now.getMonth() - back, 1);
    months.push({
      key: `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}`,
      label: date.toLocaleDateString(locale, { month: "short", year: "numeric" }),
      shortLabel: date.toLocaleDateString(locale, { month: "short" }),
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
  const { t, i18n } = useTranslation();
  const months = useMemo(() => bucketize(logs, i18n.language), [logs, i18n.language]);
  const total = months.reduce((sum, month) => sum + month.count, 0);

  if (total === 0) {
    return null;
  }

  return (
    <MonthlyBarChart
      data={months}
      ariaLabel={t("chart.monthlyWaterings", { months: MONTHS_SHOWN })}
      className="h-28"
    />
  );
}
