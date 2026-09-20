import { Stethoscope } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { DiagnosisEntry } from "@/api/types";

export function DiagnosticsCard({ checklist, commonName }: { checklist: string; commonName: string }) {
  const { t } = useTranslation();
  let entries: DiagnosisEntry[];
  try {
    const parsed: unknown = JSON.parse(checklist);
    if (!Array.isArray(parsed)) return null;
    entries = parsed as DiagnosisEntry[];
  } catch {
    return null;
  }

  if (entries.length === 0) return null;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Stethoscope className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
          {t("diagnostics.title", { name: commonName })}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-2">
        {entries.map((entry) => (
          <details key={entry.symptom} className="rounded-md border">
            <summary className="cursor-pointer px-3 py-2 text-sm font-medium">{entry.symptom}</summary>
            <ul className="list-disc space-y-1 px-6 pb-3 text-sm text-muted-foreground">
              {entry.causes.map((cause) => (
                <li key={cause}>{cause}</li>
              ))}
            </ul>
          </details>
        ))}
      </CardContent>
    </Card>
  );
}
