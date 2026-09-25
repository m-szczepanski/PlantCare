import { useTranslation } from "react-i18next";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { PlantCardSkeletonGrid } from "@/components/PlantCardSkeleton";
import { useInsights } from "@/hooks/useInsights";
import { cn } from "@/lib/utils";

function monthLabel(key: string, locale: string): string {
  const [year, month] = key.split("-").map(Number);
  return new Date(year, month - 1, 1).toLocaleDateString(locale, { month: "short" });
}

function Tile({ label, value, sub }: { label: string; value: string | number; sub?: string }) {
  return (
    <div role="group" aria-label={`${label}: ${value}`} className="px-4 py-3">
      <div className="text-2xl font-bold leading-none">{value}</div>
      <div className="mt-1 text-xs text-muted-foreground">
        {label}
        {sub ? ` · ${sub}` : ""}
      </div>
    </div>
  );
}

export function InsightsPage() {
  const { t, i18n } = useTranslation();
  const { data: insights, isPending, isError, error } = useInsights();

  if (isPending) {
    return (
      <div className="space-y-4">
        <h1 className="text-3xl font-bold">{t("insights.title")}</h1>
        <PlantCardSkeletonGrid count={3} label={t("insights.loading")} />
      </div>
    );
  }

  if (isError || !insights) {
    return (
      <Card>
        <CardContent className="pt-6 text-destructive">
          {t("insights.loadError", { message: (error as Error)?.message ?? t("common.unknownError") })}
        </CardContent>
      </Card>
    );
  }

  const maxMonthly = Math.max(...insights.monthlyWaterings.map((m) => m.count), 1);

  return (
    <div className="space-y-4">
      <Breadcrumbs items={[{ label: t("common.home"), to: "/" }, { label: t("insights.title") }]} />
      <h1 className="text-3xl font-bold">{t("insights.title")}</h1>

      <Card>
        <CardContent className="p-0">
          <div className="grid grid-cols-2 divide-x sm:grid-cols-4">
            <Tile label={t("stats.plants")} value={insights.totalPlants} />
            <Tile
              label={t("insights.scheduled")}
              value={insights.scheduledPlants}
              sub={t("insights.withoutSchedule", { count: insights.unscheduledPlants })}
            />
            <Tile label={t("insights.species")} value={insights.speciesCount} />
            <Tile
              label={t("insights.adherence", { days: insights.adherenceWindowDays })}
              value={
                insights.expectedWateringsInWindow === 0
                  ? "—"
                  : `${Math.round(insights.adherencePercent)}%`
              }
              sub={
                insights.expectedWateringsInWindow === 0
                  ? undefined
                  : t("insights.wateringsRatio", { actual: insights.actualWateringsInWindow, expected: insights.expectedWateringsInWindow })
              }
            />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t("insights.monthlyTitle")}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex h-32 items-end gap-1.5" aria-hidden="true">
            {insights.monthlyWaterings.map((month) => (
              <div
                key={month.month}
                className="flex flex-1 flex-col items-center justify-end gap-1"
              >
                <div
                  className={cn(
                    "w-full rounded",
                    month.count === 0 ? "h-px bg-border" : "bg-chart-1",
                  )}
                  style={{
                    height: month.count === 0 ? 1 : `${Math.max(8, (month.count / maxMonthly) * 100)}%`,
                  }}
                  title={`${month.month}: ${month.count}`}
                />
                <span className="text-[10px] text-muted-foreground">{monthLabel(month.month, i18n.language)}</span>
              </div>
            ))}
          </div>
          <table className="sr-only">
            <thead>
              <tr>
                <th scope="col">{t("chart.month")}</th>
                <th scope="col">{t("chart.waterings")}</th>
              </tr>
            </thead>
            <tbody>
              {insights.monthlyWaterings.map((month) => (
                <tr key={month.month}>
                  <td>{month.month}</td>
                  <td>{month.count}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle>{t("insights.neglected")}</CardTitle>
          </CardHeader>
          <CardContent>
            {insights.mostNeglected.length === 0 ? (
              <p className="text-sm text-muted-foreground">{t("insights.neglectedEmpty")}</p>
            ) : (
              <ol className="space-y-1 text-sm">
                {insights.mostNeglected.map((entry, index) => (
                  <li key={entry.plantId} className="flex justify-between gap-2">
                    <span>
                      {index + 1}. {entry.nickName}
                    </span>
                    <span className="text-muted-foreground">
                      {entry.daysSinceLastWatering === 0 ? t("form.today") : t("insights.daysAgo", { count: entry.daysSinceLastWatering })}
                    </span>
                  </li>
                ))}
              </ol>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t("insights.diversity")}</CardTitle>
          </CardHeader>
          <CardContent>
            {insights.species.length === 0 ? (
              <p className="text-sm text-muted-foreground">{t("insights.diversityEmpty")}</p>
            ) : (
              <ul className="space-y-1 text-sm">
                {insights.species.map((species) => (
                  <li key={species.plantProfileId} className="flex justify-between gap-2">
                    <span>{species.commonName}</span>
                    <span className="text-muted-foreground">×{species.plantCount}</span>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t("insights.streaks")}</CardTitle>
          </CardHeader>
          <CardContent>
            {insights.streaks.length === 0 ? (
              <p className="text-sm text-muted-foreground">{t("insights.streaksEmpty")}</p>
            ) : (
              <ul className="space-y-1 text-sm">
                {insights.streaks.map((streak) => (
                  <li key={streak.plantId} className="flex justify-between gap-2">
                    <span>{streak.nickName}</span>
                    <span className="text-muted-foreground">
                      {t("insights.onTime", { count: streak.consecutiveOnTimeWaterings })}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

export default InsightsPage;
