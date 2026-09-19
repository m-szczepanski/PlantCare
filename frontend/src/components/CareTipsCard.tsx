import Markdown from "react-markdown";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { LightRequirement, PlantCareTips } from "@/api/types";

const lightLabels: Record<LightRequirement, string> = {
  Low: "Low light",
  Medium: "Medium light",
  Bright: "Bright, indirect light",
  DirectSun: "Direct sun",
};

export function CareTipsCard({ tips }: { tips: PlantCareTips }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Care tips — {tips.commonName}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4 text-sm">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <dt className="text-muted-foreground">Light</dt>
            <dd className="font-medium">{lightLabels[tips.lightRequirement]}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">Humidity</dt>
            <dd className="font-medium">{tips.humidityNotes}</dd>
          </div>
        </div>
        <div>
          <dt className="text-muted-foreground">Care notes</dt>
          <dd className="prose prose-sm prose-neutral mt-1 max-w-none dark:prose-invert">
            <Markdown>{tips.careTips}</Markdown>
          </dd>
        </div>
      </CardContent>
    </Card>
  );
}
