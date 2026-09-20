import { useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { CircleCheck, CircleX } from "lucide-react";
import { toast } from "sonner";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { backupApi } from "@/api/client";
import { useStatus } from "@/hooks/useStatus";
import { toastError } from "@/lib/toast";
import { formatInstant } from "@/lib/dates";
import { touchButton } from "@/lib/ui";
import type { ImportResult } from "@/api/types";

function readText(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(reader.error);
    reader.readAsText(file);
  });
}



export function StatusPage() {
  const { t } = useTranslation();
  const outcomeLabels: Record<string, string> = {
    "digest-sent": t("status.outcome.digestSent"),
    "nothing-due": t("status.outcome.nothingDue"),
    "already-sent-today": t("status.outcome.alreadySent"),
    "delivery-failed": t("status.outcome.deliveryFailed"),
  };
  const { data: status, isPending, isError, error } = useStatus(30_000);
  const [importing, setImporting] = useState(false);
  const importInputRef = useRef<HTMLInputElement>(null);

  async function handleImport(file: File) {
    setImporting(true);
    try {
      const document: unknown = JSON.parse(await readText(file));
      const result = await backupApi.importDocument(document);
      toast.success(t("status.importFinished"), { description: describeImport(result, t) });
    } catch (err) {
      toastError(t("status.importFailed"), err);
    } finally {
      setImporting(false);
    }
  }

  return (
    <div className="space-y-4">
      <Breadcrumbs items={[{ label: t("common.home"), to: "/" }, { label: t("status.title") }]} />
      <h1 className="text-2xl font-bold">{t("status.title")}</h1>

      {isPending ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {Array.from({ length: 4 }, (_, i) => (
            <Skeleton key={i} className="h-32" />
          ))}
        </div>
      ) : isError || !status ? (
        <Card>
          <CardContent className="pt-6 text-destructive">
            {t("status.loadError", { message: (error as Error)?.message ?? t("common.unknownError") })}
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle>{t("status.scheduler")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-1 text-sm">
              <p>
                {t("status.cron")} <span className="font-mono">{status.wateringCheckCron}</span>
              </p>
              {status.lastJobRun ? (
                <p className="text-muted-foreground">
                  {t("status.lastRun", { time: formatInstant(status.lastJobRun.ranAt) })} —{" "}
                  {outcomeLabels[status.lastJobRun.outcome] ?? status.lastJobRun.outcome}
                </p>
              ) : (
                <p className="text-muted-foreground">{t("status.noRuns")}</p>
              )}
              {status.lastDigest ? (
                <p className="text-muted-foreground">
                  {t("status.lastDigest", {
                    count: status.lastDigest.plantCount,
                    overdue: status.lastDigest.overdueCount > 0 ? t("status.lastDigestOverdue", { count: status.lastDigest.overdueCount }) : "",
                    priority: status.lastDigest.priority,
                    time: formatInstant(status.lastDigest.sentAt),
                  })}
                </p>
              ) : null}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t("status.notifications")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <p className="flex items-center gap-2">
                {status.ntfy.reachable ? (
                  <CircleCheck className="h-4 w-4 text-primary" aria-hidden="true" />
                ) : (
                  <CircleX className="h-4 w-4 text-destructive" aria-hidden="true" />
                )}
                <span>{status.ntfy.reachable ? t("status.reachable", { ms: status.ntfy.latencyMs }) : t("status.unreachable")}</span>
              </p>
              <p className="text-muted-foreground">
                {t("status.server")} <span className="font-mono">{status.ntfy.baseUrl}</span> · {t("status.topic")}{" "}
                <span className="font-mono">{status.ntfy.topic}</span>
              </p>
              {status.ntfy.error ? (
                <p className="text-muted-foreground">{status.ntfy.error}</p>
              ) : null}
              <Button variant="outline" asChild className={touchButton}>
                <a href={status.ntfy.subscribeUrl} target="_blank" rel="noreferrer">
                  {t("status.openSubscription")}
                </a>
              </Button>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t("status.backup")}</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-wrap items-center gap-2 text-sm">
              <Button asChild className={touchButton}>
                <a href={backupApi.exportUrl} download>
                  {t("status.exportJson")}
                </a>
              </Button>
              <input
                ref={importInputRef}
                type="file"
                accept="application/json"
                className="hidden"
                disabled={importing}
                onChange={(event) => {
                  const file = event.target.files?.[0];
                  if (file) {
                    void handleImport(file);
                  }
                  event.target.value = "";
                }}
              />
              <Button
                variant="outline"
                className={touchButton}
                disabled={importing}
                onClick={() => importInputRef.current?.click()}
              >
                {importing ? t("status.importing") : t("status.importJson")}
              </Button>
              <p className="w-full text-muted-foreground">
                {t("status.importHint")}
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t("status.instance")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-1 text-sm text-muted-foreground">
              <p>{t("status.serverTime", { time: formatInstant(status.nowUtc) })}</p>
              <p>{t("status.serverTz", { zone: status.timeZoneId })}</p>
              <p>{t("status.clientTz", { zone: Intl.DateTimeFormat().resolvedOptions().timeZone })}</p>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}

function describeImport(result: ImportResult, t: (key: string, options?: Record<string, unknown>) => string): string {
  return [
    t("status.importPlants", { count: result.plantsCreated }),
    result.plantsSkipped > 0 ? t("status.importSkipped", { count: result.plantsSkipped }) : null,
    t("status.importRoomsProfiles", { rooms: result.roomsCreated, profiles: result.profilesCreated }),
  ]
    .filter(Boolean)
    .join(" · ");
}

export default StatusPage;
