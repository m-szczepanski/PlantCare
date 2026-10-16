import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { devicesApi, plantsApi } from "@/api/client";
import type { DeviceInput } from "@/api/types";
import { toastError } from "@/lib/toast";
import i18n from "@/i18n";
import { useStatus } from "@/hooks/useStatus";
import { plantKeys } from "@/hooks/usePlants";
import { dashboardKeys } from "@/hooks/useDashboard";

export const deviceKeys = {
  all: ["devices"] as const,
};

export function useDevices(enabled = true) {
  return useQuery({
    queryKey: deviceKeys.all,
    queryFn: () => devicesApi.list(),
    enabled,
  });
}

export function useDevicesEnabled() {
  const { data } = useStatus();
  return data?.devicesEnabled ?? false;
}

export function useCreateDevice() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: DeviceInput) => devicesApi.create(input),
    onSuccess: (device) => {
      queryClient.invalidateQueries({ queryKey: deviceKeys.all });
      toast.success(i18n.t("toasts.deviceCreated"), { description: i18n.t("toasts.deviceAddedDesc", { name: device.name }) });
    },
    onError: (error) => toastError(i18n.t("toasts.deviceCreateFailed"), error),
  });
}

export function useUpdateDevice() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: number; input: DeviceInput }) => devicesApi.update(id, input),
    onSuccess: (device) => {
      queryClient.invalidateQueries({ queryKey: deviceKeys.all });
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      toast.success(i18n.t("toasts.deviceUpdated"), { description: i18n.t("toasts.deviceSavedDesc", { name: device.name }) });
    },
    onError: (error) => toastError(i18n.t("toasts.deviceUpdateFailed"), error),
  });
}

export function useDeleteDevice() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => devicesApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: deviceKeys.all });
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      toast.success(i18n.t("toasts.deviceDeleted"), { description: i18n.t("toasts.deviceDeletedDesc") });
    },
    onError: (error) => toastError(i18n.t("toasts.deviceDeleteFailed"), error),
  });
}

export function useWaterNow(plantId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (durationMs?: number) => plantsApi.deviceWater(plantId, durationMs),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      queryClient.invalidateQueries({ queryKey: dashboardKeys.all });
      toast.success(i18n.t("toasts.waterNowSent"));
    },
    onError: (error) => toastError(i18n.t("toasts.waterNowFailed"), error),
  });
}
