import { useQuery } from "@tanstack/react-query";
import { healthApi } from "@/api/client";

export function useStatus(refetchIntervalMs?: number) {
  return useQuery({
    queryKey: ["status"],
    queryFn: () => healthApi.status(),
    refetchInterval: refetchIntervalMs,
  });
}
