import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { plantsApi } from "@/api/client";
import type { PlantInput } from "@/api/types";
import { toastError } from "@/lib/toast";

export const plantKeys = {
  all: ["plants"] as const,
  detail: (id: number) => ["plants", id] as const,
  wateringLogs: (id: number) => ["plants", id, "watering-logs"] as const,
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
    onSuccess: (plant) => {
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      toast.success("Plant added", { description: `${plant.nickName} is on the list.` });
    },
    onError: (error) => toastError("Could not add plant", error),
  });
}

export function useUpdatePlant() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: number; input: PlantInput }) => plantsApi.update(id, input),
    onSuccess: (plant) => {
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      queryClient.setQueryData(plantKeys.detail(plant.id), plant);
      toast.success("Plant updated", { description: `${plant.nickName} saved.` });
    },
    onError: (error) => toastError("Could not update plant", error),
  });
}

export function useDeletePlant() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => plantsApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      toast.success("Plant deleted");
    },
    onError: (error) => toastError("Could not delete plant", error),
  });
}

export function useWaterPlant() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, note }: { id: number; note?: string }) => plantsApi.water(id, note),
    onSuccess: (plant) => {
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      queryClient.setQueryData(plantKeys.detail(plant.id), plant);
      toast.success("Watered", { description: `${plant.nickName} logged.` });
    },
    onError: (error) => toastError("Could not log watering", error),
  });
}

export function useWateringLogs(id: number) {
  return useQuery({
    queryKey: plantKeys.wateringLogs(id),
    queryFn: () => plantsApi.wateringLogs(id),
    enabled: Number.isInteger(id),
  });
}

export function useUploadPlantPhoto(id: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => plantsApi.uploadPhoto(id, file),
    onSuccess: (plant) => {
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      queryClient.setQueryData(plantKeys.detail(plant.id), plant);
      toast.success("Photo uploaded", { description: `${plant.nickName} updated.` });
    },
    onError: (error) => toastError("Could not upload photo", error),
  });
}
