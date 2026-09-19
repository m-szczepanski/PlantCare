import { useQuery } from "@tanstack/react-query";
import { insightsApi } from "@/api/client";

export function useInsights() {
  return useQuery({
    queryKey: ["insights"],
    queryFn: () => insightsApi.get(),
  });
}
