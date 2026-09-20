import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Sprout } from "lucide-react";
import { cn } from "@/lib/utils";

interface PlantPhotoProps {
  photoUrl: string | null;
  nickName: string;
  className?: string;
}

export function PlantPhoto({ photoUrl, nickName, className }: PlantPhotoProps) {
  const { t } = useTranslation();
  const [failed, setFailed] = useState(false);

  if (photoUrl && !failed) {
    return (
      <img
        src={photoUrl}
        alt={nickName}
        loading="lazy"
        onError={() => setFailed(true)}
        className={cn("rounded-lg bg-muted object-cover", className)}
      />
    );
  }

  return (
    <div
      role="img"
      aria-label={t("plant.noPhoto", { name: nickName })}
      className={cn(
        "flex items-center justify-center rounded-lg border border-dashed border-sidebar-border bg-muted",
        className,
      )}
    >
      <Sprout className="h-1/3 w-1/3 text-primary/70" aria-hidden="true" />
    </div>
  );
}
