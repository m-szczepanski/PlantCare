import { useRef, useState } from "react";
import { CircleCheck, CircleX } from "lucide-react";
import { toast } from "sonner";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { backupApi } from "@/api/client";
import { useStatus } from "@/hooks/useStatus";
import { toastError } from "@/lib/toast";
import { touchButton } from "@/lib/ui";
import type { ImportResult } from "@/api/types";

const outcomeLabels: Record<string, string> = {
  "digest-sent": "digest sent",
  "nothing-due": "nothing due",
  "already-sent-today": "digest already sent today",
  "delivery-failed": "delivery failed",
};

function formatInstant(value: string): string {
  const date = new Date(value.endsWith("Z") ? value : `${value}Z`);
  return date.toLocaleString();
}

export function StatusPage() {
  const { data: status, isPending, isError, error } = useStatus(30_000);
  const [importing, setImporting] = useState(false);
  const importInputRef = useRef<HTMLInputElement>(null);

  async function handleImport(file: File) {
    setImporting(true);
    try {
      const document: unknown = JSON.parse(await new Response(file).text());
      const result = await backupApi.importDocument(document);
      toast.success("Import finished", { description: describeImport(result) });
    } catch (err) {
      toastError("Import failed", err);
    } finally {
      setImporting(false);
    }
  }

  return (
    <div className="space-y-4">
      <Breadcrumbs items={[{ label: "Home", to: "/" }, { label: "Status" }]} />
      <h1 className="text-2xl font-bold">Status</h1>

      {isPending ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {Array.from({ length: 4 }, (_, i) => (
            <Skeleton key={i} className="h-32" />
          ))}
        </div>
      ) : isError || !status ? (
        <Card>
          <CardContent className="pt-6 text-destructive">
            Could not load status: {(error as Error)?.message ?? "Unknown error"}
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle>Scheduler</CardTitle>
            </CardHeader>
            <CardContent className="space-y-1 text-sm">
              <p>
                Watering check cron: <span className="font-mono">{status.wateringCheckCron}</span>
              </p>
              {status.lastJobRun ? (
                <p className="text-muted-foreground">
                  Last run {formatInstant(status.lastJobRun.ranAt)} —{" "}
                  {outcomeLabels[status.lastJobRun.outcome] ?? status.lastJobRun.outcome}
                </p>
              ) : (
                <p className="text-muted-foreground">The job has not run since startup tracking began.</p>
              )}
              {status.lastDigest ? (
                <p className="text-muted-foreground">
                  Last digest: {status.lastDigest.plantCount} plant(s)
                  {status.lastDigest.overdueCount > 0
                    ? `, ${status.lastDigest.overdueCount} overdue`
                    : ""}{" "}
                  at priority {status.lastDigest.priority} on{" "}
                  {formatInstant(status.lastDigest.sentAt)}
                </p>
              ) : null}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Notifications (ntfy)</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <p className="flex items-center gap-2">
                {status.ntfy.reachable ? (
                  <CircleCheck className="h-4 w-4 text-primary" aria-hidden="true" />
                ) : (
                  <CircleX className="h-4 w-4 text-destructive" aria-hidden="true" />
                )}
                <span>{status.ntfy.reachable ? `Reachable (${status.ntfy.latencyMs} ms)` : "Unreachable"}</span>
              </p>
              <p className="text-muted-foreground">
                Server <span className="font-mono">{status.ntfy.baseUrl}</span> · topic{" "}
                <span className="font-mono">{status.ntfy.topic}</span>
              </p>
              {status.ntfy.error ? (
                <p className="text-muted-foreground">{status.ntfy.error}</p>
              ) : null}
              <Button variant="outline" asChild className={touchButton}>
                <a href={status.ntfy.subscribeUrl} target="_blank" rel="noreferrer">
                  Open subscription page
                </a>
              </Button>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Backup</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-wrap items-center gap-2 text-sm">
              <Button asChild className={touchButton}>
                <a href={backupApi.exportUrl} download>
                  Export JSON
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
                {importing ? "Importing..." : "Import JSON"}
              </Button>
              <p className="w-full text-muted-foreground">
                Import merges by name: existing rooms, profiles and plants are kept, only new
                records are added.
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Instance</CardTitle>
            </CardHeader>
            <CardContent className="space-y-1 text-sm text-muted-foreground">
              <p>Server time (UTC): {formatInstant(status.nowUtc)}</p>
              <p>Server time zone: {status.timeZoneId}</p>
              <p>Client time zone: {Intl.DateTimeFormat().resolvedOptions().timeZone}</p>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}

function describeImport(result: ImportResult): string {
  return [
    `${result.plantsCreated} plant(s) added`,
    result.plantsSkipped > 0 ? `${result.plantsSkipped} skipped` : null,
    `${result.roomsCreated} room(s), ${result.profilesCreated} profile(s) added`,
  ]
    .filter(Boolean)
    .join(" · ");
}

export default StatusPage;
