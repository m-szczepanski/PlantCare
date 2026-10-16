import { useTranslation } from "react-i18next";
import { cn } from "@/lib/utils";

export interface MonthlyBarDatum {
  key: string;
  label: string;
  shortLabel: string;
  count: number;
}

export function MonthlyBarChart({
  data,
  ariaLabel,
  className,
}: {
  data: MonthlyBarDatum[];
  ariaLabel: string;
  className?: string;
}) {
  const { t } = useTranslation();
  const max = Math.max(...data.map((datum) => datum.count), 1);

  return (
    <figure className="space-y-2" aria-label={ariaLabel}>
      <div className={cn("flex items-end gap-2", className)} aria-hidden="true">
        {data.map((datum) => (
          <div key={datum.key} className="flex flex-1 flex-col items-center justify-end gap-1">
            <span className="text-xs font-medium text-muted-foreground">{datum.count}</span>
            <div
              className={cn(
                "w-full rounded",
                datum.count === 0 ? "h-px bg-border" : "bg-chart-1 transition-all",
              )}
              style={{
                height: datum.count === 0 ? 1 : `${Math.max(8, (datum.count / max) * 100)}%`,
              }}
              title={t("chart.monthCount", { month: datum.label, count: datum.count })}
            />
            <span className="text-xs text-muted-foreground">{datum.shortLabel}</span>
          </div>
        ))}
      </div>
      <figcaption className="sr-only">
        <table>
          <thead>
            <tr>
              <th scope="col">{t("chart.month")}</th>
              <th scope="col">{t("chart.waterings")}</th>
            </tr>
          </thead>
          <tbody>
            {data.map((datum) => (
              <tr key={datum.key}>
                <td>{datum.label}</td>
                <td>{datum.count}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </figcaption>
    </figure>
  );
}
