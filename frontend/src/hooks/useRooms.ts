import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { roomsApi } from "@/api/client";
import type { RoomInput } from "@/api/types";
import { toastError } from "@/lib/toast";

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
      toast.success("Room created", { description: `${room.name} added.` });
    },
    onError: (error) => toastError("Could not create room", error),
  });
}

export function useUpdateRoom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: number; input: RoomInput }) => roomsApi.update(id, input),
    onSuccess: (room) => {
      queryClient.invalidateQueries({ queryKey: roomKeys.all });
      queryClient.invalidateQueries({ queryKey: ["plants"] });
      toast.success("Room updated", { description: `${room.name} saved.` });
    },
    onError: (error) => toastError("Could not update room", error),
  });
}

export function useDeleteRoom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => roomsApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: roomKeys.all });
      queryClient.invalidateQueries({ queryKey: ["plants"] });
      toast.success("Room deleted", { description: "Its plants are now room-less." });
    },
    onError: (error) => toastError("Could not delete room", error),
  });
}
