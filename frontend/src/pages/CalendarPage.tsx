import { useMemo, useState } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { PlantCardSkeletonGrid } from "@/components/PlantCardSkeleton";
import { usePlants } from "@/hooks/usePlants";
import { dateKey, projectOccurrences } from "@/lib/scheduleProjection";
import { cn } from "@/lib/utils";

const WEEKDAYS = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

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
  const today = date && startOfDay(new Date()).getTime() === date.getTime();

  return (
    <div
      aria-label={
        date
          ? `${date.toLocaleDateString(undefined, { month: "short", day: "numeric" })}: ${
              names.length > 0 ? names.join(", ") : "no waterings"
            }`
          : "empty day"
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
              <li className="text-xs text-muted-foreground">+{names.length - 3} more</li>
            ) : null}
          </ul>
        </>
      ) : null}
    </div>
  );
}

export function CalendarPage() {
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
      monthLabel: anchor.toLocaleDateString(undefined, { month: "long", year: "numeric" }),
      cells,
    };
  }, [view, anchor]);

  if (isPending) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-bold">Calendar</h1>
        <PlantCardSkeletonGrid count={3} label="Loading calendar..." />
      </div>
    );
  }

  if (isError || !plants) {
    return (
      <Card>
        <CardContent className="pt-6 text-destructive">
          Could not load calendar: {(error as Error)?.message ?? "Unknown error"}
        </CardContent>
      </Card>
    );
  }

  const occurrences = projectOccurrences(plants, from, to);
  const today = startOfDay(new Date());

  return (
    <div className="space-y-4">
      <Breadcrumbs items={[{ label: "Home", to: "/" }, { label: "Calendar" }]} />
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Calendar</h1>
        <div className="flex items-center gap-2">
          {view === "month" ? (
            <>
              <Button
                variant="outline"
                size="icon"
                aria-label="Previous month"
                onClick={() => setAnchor(new Date(anchor.getFullYear(), anchor.getMonth() - 1, 1))}
              >
                <ChevronLeft />
              </Button>
              <span className="min-w-36 text-center text-sm font-medium">{monthLabel}</span>
              <Button
                variant="outline"
                size="icon"
                aria-label="Next month"
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
            Week
          </Button>
          <Button
            variant={view === "month" ? "default" : "outline"}
            aria-pressed={view === "month"}
            onClick={() => {
              setView("month");
              setAnchor(startOfDay(new Date()));
            }}
          >
            Month
          </Button>
        </div>
      </div>

      <Card>
        <CardContent className="p-4">
          <div className="mb-2 grid grid-cols-7 gap-2 text-center text-xs font-medium text-muted-foreground">
            {WEEKDAYS.map((day) => (
              <div key={day}>{day}</div>
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
