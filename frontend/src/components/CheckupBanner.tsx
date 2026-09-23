import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Stethoscope } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import type { Plant } from "@/api/types";

export function CheckupBanner({ plants }: { plants: Plant[] }) {
  const { t } = useTranslation();

  if (plants.length === 0) {
    return null;
  }

  return (
    <Card className="border-primary/40 bg-primary/5">
      <CardContent className="flex flex-wrap items-center gap-x-3 gap-y-2 py-4">
        <span className="flex items-center gap-2 font-medium">
          <Stethoscope className="h-4 w-4 text-primary" aria-hidden="true" />
          {t("health.bannerTitle")}
        </span>
        <span className="text-sm text-muted-foreground">
          {t("health.bannerDescription", { count: plants.length })}
        </span>
        <ul className="flex flex-wrap gap-2" aria-label={t("health.bannerAria")}>
          {plants.map((plant) => (
            <li key={plant.id}>
              <Link
                to={`/plants/${plant.id}`}
                className="rounded-full border bg-background px-3 py-1 text-sm hover:bg-accent"
              >
                {plant.nickName}
              </Link>
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  );
}
