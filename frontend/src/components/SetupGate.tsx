import { useEffect, useState, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { useQueryClient } from "@tanstack/react-query";
import { SetupWizard } from "@/components/SetupWizard";
import { usePlants } from "@/hooks/usePlants";
import { useRooms } from "@/hooks/useRooms";

export function SetupGate({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const plants = usePlants();
  const rooms = useRooms();
  const [firstRun, setFirstRun] = useState<boolean | null>(null);

  useEffect(() => {
    if (firstRun !== null) return;
    if (plants.isPending || rooms.isPending) return;
    if (plants.isError || rooms.isError) {
      setFirstRun(false);
      return;
    }
    setFirstRun((plants.data?.length ?? 0) === 0 && (rooms.data?.length ?? 0) === 0);
  }, [firstRun, plants.isPending, plants.isError, plants.data, rooms.isPending, rooms.isError, rooms.data]);

  if (firstRun === null) {
    return (
      <div role="status" className="flex min-h-screen items-center justify-center text-sm text-muted-foreground">
        {t("setup.checking")}
      </div>
    );
  }

  if (firstRun) {
    return (
      <SetupWizard
        onFinished={() => {
          queryClient.invalidateQueries();
          setFirstRun(false);
        }}
      />
    );
  }

  return <>{children}</>;
}
