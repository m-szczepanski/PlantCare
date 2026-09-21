import { useQuery } from "@tanstack/react-query";
import { referenceDataApi } from "@/api/client";

export const soilTypeKeys = { all: ["reference-data", "soil-types"] as const };

export function useSoilTypes() {
  return useQuery({
    queryKey: soilTypeKeys.all,
    queryFn: () => referenceDataApi.soilTypes(),
    staleTime: 60 * 60 * 1000,
  });
}
