import type { ComponentType } from "react";
import { HeartCrack, Sparkles, ThumbsDown, ThumbsUp } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Badge } from "@/components/ui/badge";
import type { HealthStatus } from "@/api/types";

interface HealthMeta {
  variant: "default" | "destructive" | "secondary" | "outline";
  icon: ComponentType<{ className?: string }>;
}

const metaByStatus: Record<HealthStatus, HealthMeta> = {
  Sick: { variant: "destructive", icon: HeartCrack },
  Bad: { variant: "outline", icon: ThumbsDown },
  Good: { variant: "secondary", icon: ThumbsUp },
  Excellent: { variant: "default", icon: Sparkles },
};

export function HealthBadge({ status }: { status: HealthStatus }) {
  const { t } = useTranslation();
  const { variant, icon: Icon } = metaByStatus[status];

  return (
    <Badge variant={variant} className="gap-1">
      <Icon className="h-3 w-3" aria-hidden="true" />
      {t(`health.status.${status}`)}
    </Badge>
  );
}
