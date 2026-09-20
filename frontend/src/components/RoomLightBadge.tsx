import { CheckCircle2, Moon, Sun, SunDim } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Badge } from "@/components/ui/badge";
import type { RoomLightMatch } from "@/api/types";

interface MatchMeta {
  labelKey: string;
  variant: "default" | "destructive" | "outline" | "secondary";
  icon: typeof Moon;
}

const metaByMatch: Record<RoomLightMatch, MatchMeta> = {
  Good: { labelKey: "roomLight.good", variant: "secondary", icon: CheckCircle2 },
  SlightlyTooDark: { labelKey: "roomLight.slightlyTooDark", variant: "outline", icon: Moon },
  SlightlyTooBright: { labelKey: "roomLight.slightlyTooBright", variant: "outline", icon: SunDim },
  MuchTooDark: { labelKey: "roomLight.muchTooDark", variant: "destructive", icon: Moon },
  MuchTooBright: { labelKey: "roomLight.muchTooBright", variant: "destructive", icon: Sun },
};

export function RoomLightBadge({ match }: { match: RoomLightMatch }) {
  const { t } = useTranslation();
  const { labelKey, variant, icon: Icon } = metaByMatch[match];

  return (
    <Badge variant={variant} className="gap-1">
      <Icon className="h-3 w-3" aria-hidden="true" />
      {t(labelKey)}
    </Badge>
  );
}
