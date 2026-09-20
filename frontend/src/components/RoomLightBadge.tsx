import { CheckCircle2, Moon, Sun, SunDim } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import type { RoomLightMatch } from "@/api/types";

interface MatchMeta {
  label: string;
  variant: "default" | "destructive" | "outline" | "secondary";
  icon: typeof Moon;
}

const metaByMatch: Record<RoomLightMatch, MatchMeta> = {
  Good: { label: "Right room", variant: "secondary", icon: CheckCircle2 },
  SlightlyTooDark: { label: "A bit dark", variant: "outline", icon: Moon },
  SlightlyTooBright: { label: "A bit bright", variant: "outline", icon: SunDim },
  MuchTooDark: { label: "Wrong room — too dark", variant: "destructive", icon: Moon },
  MuchTooBright: { label: "Wrong room — too bright", variant: "destructive", icon: Sun },
};

export function RoomLightBadge({ match }: { match: RoomLightMatch }) {
  const { label, variant, icon: Icon } = metaByMatch[match];

  return (
    <Badge variant={variant} className="gap-1">
      <Icon className="h-3 w-3" aria-hidden="true" />
      {label}
    </Badge>
  );
}
