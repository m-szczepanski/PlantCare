import { useQuery } from "@tanstack/react-query";
import { referenceDataApi } from "@/api/client";

export const soilMixKeys = { all: ["reference-data", "soil-mixes"] as const };

export function useSoilMixes() {
  return useQuery({
    queryKey: soilMixKeys.all,
    queryFn: () => referenceDataApi.soilMixes(),
    staleTime: 60 * 60 * 1000,
  });
}
