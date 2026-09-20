import { useTranslation } from "react-i18next";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export function PlantDetailSkeleton() {
  const { t } = useTranslation();
  return (
    <div role="status">
      <span className="sr-only">{t("common.loadingPlant")}</span>
      <div className="space-y-4" aria-hidden="true">
        <div className="flex items-center justify-between gap-2">
          <Skeleton className="h-8 w-1/3" />
          <Skeleton className="h-5 w-24 rounded-full" />
        </div>
        <Card>
          <CardHeader>
            <Skeleton className="h-6 w-24" />
          </CardHeader>
          <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            {Array.from({ length: 6 }, (_, index) => (
              <Skeleton key={index} className="h-9" />
            ))}
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <Skeleton className="h-6 w-36" />
          </CardHeader>
          <CardContent className="space-y-2">
            {Array.from({ length: 3 }, (_, index) => (
              <Skeleton key={index} className="h-4" />
            ))}
          </CardContent>
        </Card>
        <div className="flex gap-2">
          {Array.from({ length: 4 }, (_, index) => (
            <Skeleton key={index} className="h-9 w-28" />
          ))}
        </div>
      </div>
    </div>
  );
}
