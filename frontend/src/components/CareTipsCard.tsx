import Markdown from "react-markdown";
import { useTranslation } from "react-i18next";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { PlantCareTips } from "@/api/types";

export function CareTipsCard({ tips }: { tips: PlantCareTips }) {
  const { t } = useTranslation();

  const lightLabels: Record<PlantCareTips["lightRequirement"], string> = {
    Low: t("light.low"),
    Medium: t("light.medium"),
    Bright: t("light.bright"),
    DirectSun: t("light.directSun"),
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("careTips.title", { name: tips.commonName })}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4 text-sm">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <dt className="text-muted-foreground">{t("careTips.light")}</dt>
            <dd className="font-medium">{lightLabels[tips.lightRequirement]}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">{t("careTips.humidity")}</dt>
            <dd className="font-medium">{tips.humidityNotes}</dd>
          </div>
        </div>
        <div>
          <dt className="text-muted-foreground">{t("careTips.notes")}</dt>
          <dd className="prose prose-sm prose-neutral mt-1 max-w-none dark:prose-invert">
            <Markdown>{tips.careTips}</Markdown>
          </dd>
        </div>
      </CardContent>
    </Card>
  );
}
