import { useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { plantsApi } from "@/api/client";
import i18n from "@/i18n";
import type { Plant, PlantInput, WaterDetails } from "@/api/types";
import { toastError } from "@/lib/toast";

export const plantKeys = {
  all: ["plants"] as const,
  detail: (id: number) => ["plants", id] as const,
  wateringLogs: (id: number) => ["plants", id, "watering-logs"] as const,
  notes: (id: number) => ["plants", id, "notes"] as const,
  journal: (id: number) => ["plants", id, "journal"] as const,
  careTasks: (id: number) => ["plants", id, "care-tasks"] as const,
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
    dueMessage: interval ? i18n.t("due.until", { count: interval }) : i18n.t("due.notScheduledLabel"),
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
      toast.success(i18n.t("toasts.plantAdded"), { description: i18n.t("toasts.plantAddedDesc", { name: plant.nickName }) });
    },
    onError: (error) => toastError(i18n.t("toasts.plantAddFailed"), error),
  });
}

export function useUpdatePlant() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: number; input: PlantInput }) => plantsApi.update(id, input),
    onSuccess: (plant) => {
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      queryClient.setQueryData(plantKeys.detail(plant.id), plant);
      toast.success(i18n.t("toasts.plantUpdated"), { description: i18n.t("toasts.plantSaved", { name: plant.nickName }) });
    },
    onError: (error) => toastError(i18n.t("toasts.plantUpdateFailed"), error),
  });
}

export function useDeletePlant() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => plantsApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      toast.success(i18n.t("toasts.plantDeleted"));
    },
    onError: (error) => toastError(i18n.t("toasts.plantDeleteFailed"), error),
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
      toastError(i18n.t("toasts.waterFailed"), error);
    },
    onSuccess: (plant) => {
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      queryClient.setQueryData(plantKeys.detail(plant.id), plant);
      toast.success(i18n.t("toasts.watered"), {
        description: i18n.t("toasts.wateredDesc", { name: plant.nickName }),
        action: { label: i18n.t("common.undo"), onClick: () => void undoWatering(plant.id, queryClient) },
      });
    },
  });
}

export function useBulkWater() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ids: number[]) => plantsApi.bulkWater(ids),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success(i18n.t("toasts.watered"), {
        description: i18n.t("toasts.bulkWateredDesc", { watered: result.watered, requested: result.requested }),
      });
    },
    onError: (error) => toastError(i18n.t("toasts.bulkWaterFailed"), error),
  });
}

async function undoWatering(id: number, queryClient: QueryClient) {
  try {
    const plant = await plantsApi.undoWater(id);
    queryClient.setQueryData(plantKeys.detail(plant.id), plant);
    queryClient.invalidateQueries({ queryKey: plantKeys.all });
    toast.success(i18n.t("toasts.waterUndone"), { description: i18n.t("toasts.waterUndoneDesc", { name: plant.nickName }) });
  } catch (error) {
    toastError(i18n.t("toasts.waterUndoFailed"), error);
  }
}

export function useWateringLogs(id: number) {
  return useQuery({
    queryKey: plantKeys.wateringLogs(id),
    queryFn: () => plantsApi.wateringLogs(id),
    enabled: Number.isInteger(id),
  });
}

export function useCareTasks(id: number) {
  return useQuery({
    queryKey: plantKeys.careTasks(id),
    queryFn: () => plantsApi.careTasks(id),
    enabled: Number.isInteger(id),
  });
}

export function useCareTaskMutations(id: number) {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: plantKeys.careTasks(id) });
    queryClient.invalidateQueries({ queryKey: plantKeys.all });
    queryClient.invalidateQueries({ queryKey: plantKeys.detail(id) });
    queryClient.invalidateQueries({ queryKey: plantKeys.wateringLogs(id) });
  };

  const add = useMutation({
    mutationFn: (input: { type: "Fertilizing" | "Repotting"; intervalDays: number; reduceInWinter?: boolean }) =>
      plantsApi.addCareTask(id, input),
    onSuccess: (task) => {
      invalidate();
      toast.success(i18n.t("toasts.careTaskAdded", { type: i18n.t(`careTask.types.${task.type}`) }));
    },
    onError: (error) => toastError(i18n.t("toasts.careTaskAddFailed"), error),
  });

  const remove = useMutation({
    mutationFn: (taskId: number) => plantsApi.deleteCareTask(id, taskId),
    onSuccess: () => {
      invalidate();
      toast.success(i18n.t("toasts.careTaskRemoved"));
    },
    onError: (error) => toastError(i18n.t("toasts.careTaskRemoveFailed"), error),
  });

  const markDone = useMutation({
    mutationFn: (type: "Watering" | "Fertilizing" | "Repotting") => plantsApi.markCareTaskDone(id, type),
    onSuccess: (task) => {
      invalidate();
      toast.success(i18n.t("toasts.careTaskDone", { type: i18n.t(`careTask.types.${task.type}`) }));
    },
    onError: (error) => toastError(i18n.t("toasts.careTaskDoneFailed"), error),
  });

  return { add, remove, markDone };
}

function invalidatePlantCaches(queryClient: QueryClient, id: number) {
  queryClient.invalidateQueries({ queryKey: plantKeys.detail(id) });
  queryClient.invalidateQueries({ queryKey: plantKeys.all });
  queryClient.invalidateQueries({ queryKey: ["dashboard"] });
}

export function useSnoozePlant(id: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (days: number) => plantsApi.snooze(id, days),
    onSuccess: (plant) => {
      queryClient.setQueryData(plantKeys.detail(plant.id), plant);
      invalidatePlantCaches(queryClient, plant.id);
      toast.success(i18n.t("toasts.snoozed"), { description: i18n.t("toasts.snoozedDesc", { name: plant.nickName }) });
    },
    onError: (error) => toastError(i18n.t("toasts.snoozeFailed"), error),
  });
}

export function useUnsnoozePlant(id: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => plantsApi.clearSnooze(id),
    onSuccess: (plant) => {
      queryClient.setQueryData(plantKeys.detail(plant.id), plant);
      invalidatePlantCaches(queryClient, plant.id);
      toast.success(i18n.t("toasts.resumed"), { description: i18n.t("toasts.resumedDesc", { name: plant.nickName }) });
    },
    onError: (error) => toastError(i18n.t("toasts.resumeFailed"), error),
  });
}

export function useSnoozeAllPlants() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (days: number) => plantsApi.snoozeAll(days),
    onSuccess: (result) => {
      invalidatePlantCaches(queryClient, -1);
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success(i18n.t("toasts.snoozedAll"), { description: i18n.t("toasts.snoozedAllDesc", { count: result.snoozedPlants }) });
    },
    onError: (error) => toastError(i18n.t("toasts.snoozeAllFailed"), error),
  });
}

export function useJournalEntries(id: number) {
  return useQuery({
    queryKey: plantKeys.journal(id),
    queryFn: () => plantsApi.journal(id),
    enabled: Number.isInteger(id),
  });
}

export function useJournalMutations(id: number) {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: plantKeys.journal(id) });

  const add = useMutation({
    mutationFn: (input: { entryDate?: string; text?: string; file?: File | null }) =>
      plantsApi.addJournalEntry(id, input),
    onSuccess: () => {
      invalidate();
      toast.success(i18n.t("toasts.journalAdded"));
    },
    onError: (error) => toastError(i18n.t("toasts.journalAddFailed"), error),
  });

  const remove = useMutation({
    mutationFn: (entryId: number) => plantsApi.deleteJournalEntry(id, entryId),
    onSuccess: () => {
      invalidate();
      toast.success(i18n.t("toasts.journalDeleted"));
    },
    onError: (error) => toastError(i18n.t("toasts.journalDeleteFailed"), error),
  });

  return { add, remove };
}

export function usePlantNotes(id: number) {
  return useQuery({
    queryKey: plantKeys.notes(id),
    queryFn: () => plantsApi.notes(id),
    enabled: Number.isInteger(id),
  });
}

export function useAddPlantNote(id: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (text: string) => plantsApi.addNote(id, text),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: plantKeys.notes(id) });
      toast.success(i18n.t("toasts.noteAdded"));
    },
    onError: (error) => toastError(i18n.t("toasts.noteAddFailed"), error),
  });
}

export function useUploadPlantPhotoToId() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, file }: { id: number; file: File }) => plantsApi.uploadPhoto(id, file),
    onSuccess: (plant) => {
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      queryClient.setQueryData(plantKeys.detail(plant.id), plant);
      toast.success(i18n.t("toasts.photoUploaded"), { description: i18n.t("toasts.photoUploadedDesc", { name: plant.nickName }) });
    },
    onError: (error) => toastError(i18n.t("toasts.photoUploadFailed"), error),
  });
}

export function useUploadPlantPhoto(id: number) {
  const mutation = useUploadPlantPhotoToId();
  return {
    ...mutation,
    mutate: (file: File) => mutation.mutate({ id, file }),
    mutateAsync: (file: File) => mutation.mutateAsync({ id, file }),
  };
}
