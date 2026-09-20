import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { roomsApi } from "@/api/client";
import type { RoomInput } from "@/api/types";
import { toastError } from "@/lib/toast";
import i18n from "@/i18n";

export const roomKeys = {
  all: ["rooms"] as const,
};

export function useRooms() {
  return useQuery({
    queryKey: roomKeys.all,
    queryFn: () => roomsApi.list(),
  });
}

export function useCreateRoom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RoomInput) => roomsApi.create(input),
    onSuccess: (room) => {
      queryClient.invalidateQueries({ queryKey: roomKeys.all });
      toast.success(i18n.t("toasts.roomCreated"), { description: i18n.t("toasts.roomAddedDesc", { name: room.name }) });
    },
    onError: (error) => toastError(i18n.t("toasts.roomCreateFailed"), error),
  });
}

export function useUpdateRoom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: number; input: RoomInput }) => roomsApi.update(id, input),
    onSuccess: (room) => {
      queryClient.invalidateQueries({ queryKey: roomKeys.all });
      queryClient.invalidateQueries({ queryKey: ["plants"] });
      toast.success(i18n.t("toasts.roomUpdated"), { description: i18n.t("toasts.roomSavedDesc", { name: room.name }) });
    },
    onError: (error) => toastError(i18n.t("toasts.roomUpdateFailed"), error),
  });
}

export function useDeleteRoom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => roomsApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: roomKeys.all });
      queryClient.invalidateQueries({ queryKey: ["plants"] });
      toast.success(i18n.t("toasts.roomDeleted"), { description: i18n.t("toasts.roomDeletedDesc") });
    },
    onError: (error) => toastError(i18n.t("toasts.roomDeleteFailed"), error),
  });
}
