import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Sprout } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { EmptyState } from "@/components/EmptyState";

export function NoPlantsEmptyState() {
  const { t } = useTranslation();
  return (
    <Card>
      <CardContent className="p-0">
        <EmptyState
          icon={Sprout}
          title={t("empty.noPlantsTitle")}
          description={t("empty.noPlantsDescription")}
          action={
            <Button asChild>
              <Link to="/plants/new">{t("empty.addFirstPlant")}</Link>
            </Button>
          }
        />
      </CardContent>
    </Card>
  );
}
