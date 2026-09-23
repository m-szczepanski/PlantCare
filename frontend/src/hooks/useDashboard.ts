import { useQuery } from "@tanstack/react-query";
import { dashboardApi } from "@/api/client";

export const dashboardKeys = {
  all: ["dashboard"] as const,
};

export function useDashboard(refetchIntervalMs?: number) {
  return useQuery({
    queryKey: dashboardKeys.all,
    queryFn: () => dashboardApi.get(),
    refetchInterval: refetchIntervalMs,
  });
}
