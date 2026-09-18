import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { plantsApi } from "@/api/client";
import type { PlantInput } from "@/api/types";

export const plantKeys = {
  all: ["plants"] as const,
  detail: (id: number) => ["plants", id] as const,
};

export function usePlants() {
  return useQuery({
    queryKey: plantKeys.all,
    queryFn: () => plantsApi.list(),
  });
}

export function usePlant(id: number) {
  return useQuery({
    queryKey: plantKeys.detail(id),
    queryFn: () => plantsApi.get(id),
    enabled: Number.isInteger(id),
  });
}

export function useCreatePlant() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: PlantInput) => plantsApi.create(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: plantKeys.all }),
  });
}

export function useUpdatePlant() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: number; input: PlantInput }) => plantsApi.update(id, input),
    onSuccess: (plant) => {
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      queryClient.setQueryData(plantKeys.detail(plant.id), plant);
    },
  });
}

export function useDeletePlant() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => plantsApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: plantKeys.all }),
  });
}
