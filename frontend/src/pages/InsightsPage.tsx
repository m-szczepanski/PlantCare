import { Breadcrumbs } from "@/components/Breadcrumbs";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { PlantCardSkeletonGrid } from "@/components/PlantCardSkeleton";
import { useInsights } from "@/hooks/useInsights";
import { cn } from "@/lib/utils";

function monthLabel(key: string): string {
  const [year, month] = key.split("-").map(Number);
  return new Date(year, month - 1, 1).toLocaleDateString(undefined, { month: "short" });
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
  const { data: insights, isPending, isError, error } = useInsights();

  if (isPending) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-bold">Insights</h1>
        <PlantCardSkeletonGrid count={3} label="Loading insights..." />
      </div>
    );
  }

  if (isError || !insights) {
    return (
      <Card>
        <CardContent className="pt-6 text-destructive">
          Could not load insights: {(error as Error)?.message ?? "Unknown error"}
        </CardContent>
      </Card>
    );
  }

  const maxMonthly = Math.max(...insights.monthlyWaterings.map((m) => m.count), 1);

  return (
    <div className="space-y-4">
      <Breadcrumbs items={[{ label: "Home", to: "/" }, { label: "Insights" }]} />
      <h1 className="text-2xl font-bold">Insights</h1>

      <Card>
        <CardContent className="p-0">
          <div className="grid grid-cols-2 divide-x sm:grid-cols-4">
            <Tile label="Plants" value={insights.totalPlants} />
            <Tile
              label="Scheduled"
              value={insights.scheduledPlants}
              sub={`${insights.unscheduledPlants} without a schedule`}
            />
            <Tile label="Species" value={insights.speciesCount} />
            <Tile
              label={`Adherence (${insights.adherenceWindowDays}d)`}
              value={
                insights.expectedWateringsInWindow === 0
                  ? "—"
                  : `${Math.round(insights.adherencePercent)}%`
              }
              sub={
                insights.expectedWateringsInWindow === 0
                  ? undefined
                  : `${insights.actualWateringsInWindow}/${insights.expectedWateringsInWindow} waterings`
              }
            />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Waterings per month</CardTitle>
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
                <span className="text-[10px] text-muted-foreground">{monthLabel(month.month)}</span>
              </div>
            ))}
          </div>
          <table className="sr-only">
            <thead>
              <tr>
                <th scope="col">Month</th>
                <th scope="col">Waterings</th>
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
            <CardTitle>Most neglected</CardTitle>
          </CardHeader>
          <CardContent>
            {insights.mostNeglected.length === 0 ? (
              <p className="text-sm text-muted-foreground">Add some plants first.</p>
            ) : (
              <ol className="space-y-1 text-sm">
                {insights.mostNeglected.map((entry, index) => (
                  <li key={entry.plantId} className="flex justify-between gap-2">
                    <span>
                      {index + 1}. {entry.nickName}
                    </span>
                    <span className="text-muted-foreground">
                      {entry.daysSinceLastWatering === 0
                        ? "today"
                        : `${entry.daysSinceLastWatering}d ago`}
                    </span>
                  </li>
                ))}
              </ol>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Species diversity</CardTitle>
          </CardHeader>
          <CardContent>
            {insights.species.length === 0 ? (
              <p className="text-sm text-muted-foreground">No plants linked to a profile yet.</p>
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
            <CardTitle>On-time streaks</CardTitle>
          </CardHeader>
          <CardContent>
            {insights.streaks.length === 0 ? (
              <p className="text-sm text-muted-foreground">Log waterings to build streaks.</p>
            ) : (
              <ul className="space-y-1 text-sm">
                {insights.streaks.map((streak) => (
                  <li key={streak.plantId} className="flex justify-between gap-2">
                    <span>{streak.nickName}</span>
                    <span className="text-muted-foreground">
                      {streak.consecutiveOnTimeWaterings} on time
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
