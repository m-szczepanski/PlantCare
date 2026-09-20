import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { PlantCardSkeletonGrid } from "@/components/PlantCardSkeleton";
import { usePlants } from "@/hooks/usePlants";
import { dateKey, projectOccurrences } from "@/lib/scheduleProjection";
import { cn } from "@/lib/utils";

const WEEKDAY_BASE = new Date(2024, 0, 1);

function weekdayLabels(locale: string): string[] {
  return Array.from({ length: 7 }, (_, index) =>
    new Date(WEEKDAY_BASE.getFullYear(), WEEKDAY_BASE.getMonth(), WEEKDAY_BASE.getDate() + index * 7).toLocaleDateString(locale, { weekday: "short" }),
  );
}

type View = "week" | "month";

function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

function DayCell({
  date,
  names,
  muted,
}: {
  date: Date | null;
  names: string[];
  muted?: boolean;
}) {
  const { t, i18n } = useTranslation();
  const today = date && startOfDay(new Date()).getTime() === date.getTime();

  return (
    <div
      aria-label={
        date
          ? `${date.toLocaleDateString(i18n.language, { month: "short", day: "numeric" })}: ${
              names.length > 0 ? names.join(", ") : t("calendar.noWaterings")
            }`
          : t("calendar.emptyDay")
      }
      className={cn(
        "min-h-24 rounded-lg border p-2 text-sm",
        !date && "border-dashed opacity-40",
        date && muted && "opacity-50",
        today && "ring-2 ring-primary",
      )}
    >
      {date ? (
        <>
          <div className="font-medium">{date.getDate()}</div>
          <ul className="mt-1 space-y-1">
            {names.slice(0, 3).map((name) => (
              <li key={name} className="truncate rounded bg-primary/10 px-1.5 py-0.5 text-xs text-primary">
                {name}
              </li>
            ))}
            {names.length > 3 ? (
              <li className="text-xs text-muted-foreground">{t("calendar.more", { count: names.length - 3 })}</li>
            ) : null}
          </ul>
        </>
      ) : null}
    </div>
  );
}

export function CalendarPage() {
  const { t, i18n } = useTranslation();
  const { data: plants, isPending, isError, error } = usePlants();
  const [view, setView] = useState<View>("week");
  const [anchor, setAnchor] = useState<Date>(() => startOfDay(new Date()));

  const { cells, from, to, monthLabel } = useMemo(() => {
    const today = startOfDay(new Date());
    if (view === "week") {
      const from = today;
      const to = addDays(from, 6);
      return {
        from,
        to,
        monthLabel: null as string | null,
        cells: Array.from({ length: 7 }, (_, index) => addDays(from, index)),
      };
    }
    const from = new Date(anchor.getFullYear(), anchor.getMonth(), 1);
    const to = new Date(anchor.getFullYear(), anchor.getMonth() + 1, 0);
    const leading = (from.getDay() + 6) % 7;
    const cells: (Date | null)[] = [
      ...Array.from({ length: leading }, () => null),
      ...Array.from({ length: to.getDate() }, (_, index) => addDays(from, index)),
    ];
    return {
      from,
      to,
      monthLabel: anchor.toLocaleDateString(i18n.language, { month: "long", year: "numeric" }),
      cells,
    };
  }, [view, anchor, i18n.language]);

  if (isPending) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-bold">{t("calendar.title")}</h1>
        <PlantCardSkeletonGrid count={3} label={t("calendar.loading")} />
      </div>
    );
  }

  if (isError || !plants) {
    return (
      <Card>
        <CardContent className="pt-6 text-destructive">
          {t("calendar.loadError", { message: (error as Error)?.message ?? t("common.unknownError") })}
        </CardContent>
      </Card>
    );
  }

  const occurrences = projectOccurrences(plants, from, to);
  const today = startOfDay(new Date());

  return (
    <div className="space-y-4">
      <Breadcrumbs items={[{ label: t("common.home"), to: "/" }, { label: t("calendar.title") }]} />
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">{t("calendar.title")}</h1>
        <div className="flex items-center gap-2">
          {view === "month" ? (
            <>
              <Button
                variant="outline"
                size="icon"
                aria-label={t("calendar.previousMonth")}
                onClick={() => setAnchor(new Date(anchor.getFullYear(), anchor.getMonth() - 1, 1))}
              >
                <ChevronLeft />
              </Button>
              <span className="min-w-36 text-center text-sm font-medium">{monthLabel}</span>
              <Button
                variant="outline"
                size="icon"
                aria-label={t("calendar.nextMonth")}
                onClick={() => setAnchor(new Date(anchor.getFullYear(), anchor.getMonth() + 1, 1))}
              >
                <ChevronRight />
              </Button>
            </>
          ) : null}
          <Button
            variant={view === "week" ? "default" : "outline"}
            aria-pressed={view === "week"}
            onClick={() => {
              setView("week");
              setAnchor(startOfDay(new Date()));
            }}
          >
            {t("calendar.week")}
          </Button>
          <Button
            variant={view === "month" ? "default" : "outline"}
            aria-pressed={view === "month"}
            onClick={() => {
              setView("month");
              setAnchor(startOfDay(new Date()));
            }}
          >
            {t("calendar.month")}
          </Button>
        </div>
      </div>

      <Card>
        <CardContent className="p-4">
          <div className="mb-2 grid grid-cols-7 gap-2 text-center text-xs font-medium text-muted-foreground">
            {weekdayLabels(i18n.language).map((day, index) => (
              <div key={index}>{day}</div>
            ))}
          </div>
          <div className="grid grid-cols-7 gap-2">
            {cells.map((cell, index) => (
              <DayCell
                key={cell ? dateKey(cell) : `empty-${index}`}
                date={cell}
                names={
                  cell
                    ? (occurrences.get(dateKey(cell)) ?? []).map((plant) => plant.nickName)
                    : []
                }
                muted={cell ? cell < today : false}
              />
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

export default CalendarPage;
