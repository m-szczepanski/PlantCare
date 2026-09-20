import { useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { plantsApi } from "@/api/client";
import type { Plant, PlantInput, WaterDetails } from "@/api/types";
import { toastError } from "@/lib/toast";

export const plantKeys = {
  all: ["plants"] as const,
  detail: (id: number) => ["plants", id] as const,
  wateringLogs: (id: number) => ["plants", id, "watering-logs"] as const,
};

function optimisticWatered(plant: Plant): Plant {
  const now = new Date();
  const interval = plant.wateringIntervalDays;
  const nextDueDate = interval ? new Date(now.getTime() + interval * 86_400_000) : null;

  return {
    ...plant,
    lastWateredAt: now.toISOString(),
    dueStatus: interval ? "Upcoming" : "NotScheduled",
    daysUntilDue: interval,
    nextDueDate: nextDueDate ? nextDueDate.toISOString() : null,
    dueMessage: interval ? `${interval} days until due` : "Not scheduled",
  };
}

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
    mutationFn: ({ id, ...details }: { id: number } & WaterDetails) =>
      plantsApi.water(id, Object.keys(details).length > 0 ? details : undefined),
    onMutate: async ({ id }) => {
      const previousList = queryClient.getQueryData<Plant[]>(plantKeys.all);
      const previousDetail = queryClient.getQueryData<Plant>(plantKeys.detail(id));
      const target = previousDetail ?? previousList?.find((p) => p.id === id);

      if (previousList) {
        queryClient.setQueryData(
          plantKeys.all,
          previousList.map((p) => (p.id === id ? optimisticWatered(p) : p)),
        );
      }
      if (target) {
        queryClient.setQueryData(plantKeys.detail(id), optimisticWatered(target));
      }

      return { previousList, previousDetail };
    },
    onError: (error, { id }, context) => {
      if (context?.previousList) {
        queryClient.setQueryData(plantKeys.all, context.previousList);
      }
      if (context?.previousDetail) {
        queryClient.setQueryData(plantKeys.detail(id), context.previousDetail);
      }
      toastError("Could not log watering", error);
    },
    onSuccess: (plant) => {
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      queryClient.setQueryData(plantKeys.detail(plant.id), plant);
      toast.success("Watered", {
        description: `${plant.nickName} logged.`,
        action: { label: "Undo", onClick: () => void undoWatering(plant.id, queryClient) },
      });
    },
  });
}

async function undoWatering(id: number, queryClient: QueryClient) {
  try {
    const plant = await plantsApi.undoWater(id);
    queryClient.setQueryData(plantKeys.detail(plant.id), plant);
    queryClient.invalidateQueries({ queryKey: plantKeys.all });
    toast.success("Watering undone", { description: `${plant.nickName} restored.` });
  } catch (error) {
    toastError("Could not undo watering", error);
  }
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
