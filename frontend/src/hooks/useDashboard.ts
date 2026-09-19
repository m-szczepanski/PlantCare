import { useQuery } from "@tanstack/react-query";
import { dashboardApi } from "@/api/client";

export function useDashboard(refetchIntervalMs?: number) {
  return useQuery({
    queryKey: ["dashboard"],
    queryFn: () => dashboardApi.get(),
    refetchInterval: refetchIntervalMs,
  });
}
