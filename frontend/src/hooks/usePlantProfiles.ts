import { useQuery } from "@tanstack/react-query";
import { plantProfilesApi } from "@/api/client";

export function usePlantProfiles() {
  return useQuery({
    queryKey: ["plant-profiles"],
    queryFn: () => plantProfilesApi.list(),
    staleTime: 5 * 60 * 1000,
  });
}
