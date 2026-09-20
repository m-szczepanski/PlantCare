import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Sprout } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useCareTaskMutations, useCareTasks } from "@/hooks/usePlants";
import { cn } from "@/lib/utils";
import { touchButton, touchField } from "@/lib/ui";
import type { CareTask } from "@/api/types";

export function CareTasksCard({ plantId }: { plantId: number }) {
  const { t } = useTranslation();
  const labels: Record<CareTask["type"], string> = {
    Watering: t("careTask.types.Watering"),
    Fertilizing: t("careTask.types.Fertilizing"),
    Repotting: t("careTask.types.Repotting"),
  };
  const { data: tasks = [] } = useCareTasks(plantId);
  const { add, remove, markDone } = useCareTaskMutations(plantId);
  const [interval, setInterval] = useState("30");
  const [reduce, setReduce] = useState(true);

  const hasFertilizing = tasks.some((task) => task.type === "Fertilizing");
  const hasRepotting = tasks.some((task) => task.type === "Repotting");
  const pendingInterval = Number(interval);

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("careTask.title")}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {tasks.map((task) => (
          <div
            key={task.id}
            className="flex flex-wrap items-center gap-x-3 gap-y-1 rounded-md border p-3 text-sm"
          >
            <span className="font-medium">{labels[task.type]}</span>
            <span
              className={cn(
                task.dueStatus === "Overdue" && "text-destructive",
                task.dueStatus !== "Overdue" && "text-muted-foreground",
              )}
            >
              {task.dueMessage}
            </span>
            {task.hint ? <span className="text-xs text-muted-foreground">{task.hint}</span> : null}
            <div className="ml-auto flex gap-2">
              <Button
                size="sm"
                variant="outline"
                disabled={markDone.isPending}
                onClick={() => markDone.mutate(task.type)}
              >
                {t("careTask.markDone")}
              </Button>
              {task.type !== "Watering" ? (
                <Button
                  size="sm"
                  variant="ghost"
                  aria-label={t("careTask.removeAria", { task: labels[task.type] })}
                  disabled={remove.isPending}
                  onClick={() => remove.mutate(task.id)}
                >
                  {t("careTask.remove")}
                </Button>
              ) : null}
            </div>
          </div>
        ))}

        {!hasRepotting ? (
          <div className="flex flex-wrap items-end gap-2 border-t pt-3">
            <span className="pb-2 text-sm text-muted-foreground">{t("careTask.repottingHint")}</span>
            <Button
              className={touchButton}
              size="sm"
              variant="secondary"
              disabled={add.isPending}
              onClick={() =>
                add.mutate({ type: "Repotting", intervalDays: 365, reduceInWinter: false })
              }
            >
              {t("careTask.addRepotting")}
            </Button>
          </div>
        ) : null}

        {!hasFertilizing ? (
          <div className={hasRepotting ? "flex flex-wrap items-end gap-2 border-t pt-3" : "flex flex-wrap items-end gap-2"}>
            <div className="w-28 space-y-1">
              <Label htmlFor="fertilizerInterval">{t("careTask.everyDays")}</Label>
              <Input
                id="fertilizerInterval"
                type="number"
                min={1}
                value={interval}
                onChange={(event) => setInterval(event.target.value)}
                className={touchField}
              />
            </div>
            <label className="flex items-center gap-2 pb-2 text-sm">
              <input
                type="checkbox"
                checked={reduce}
                onChange={(event) => setReduce(event.target.checked)}
                className="h-4 w-4 rounded border-input accent-primary"
              />
              {t("careTask.skipWinter")}
            </label>
            <Button
              className={`${touchButton} mb-0.5`}
              size="sm"
              variant="secondary"
              disabled={
                add.isPending ||
                !Number.isInteger(pendingInterval) ||
                pendingInterval < 1 ||
                pendingInterval > 3650
              }
              onClick={() =>
                add.mutate({ type: "Fertilizing", intervalDays: pendingInterval, reduceInWinter: reduce })
              }
            >
              <Sprout className="mr-1 h-4 w-4" aria-hidden="true" />
              {t("careTask.addFertilizing")}
            </Button>
          </div>
        ) : null}
      </CardContent>
    </Card>
  );
}
