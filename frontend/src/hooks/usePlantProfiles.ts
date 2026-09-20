import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { plantProfilesApi } from "@/api/client";
import type { PlantProfileInput } from "@/api/types";
import { toastError } from "@/lib/toast";

export const profileKeys = { all: ["plant-profiles"] as const };

export function usePlantProfiles() {
  return useQuery({
    queryKey: profileKeys.all,
    queryFn: () => plantProfilesApi.list(),
    staleTime: 5 * 60 * 1000,
  });
}

export function useProfileMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: profileKeys.all });

  const create = useMutation({
    mutationFn: (input: PlantProfileInput) => plantProfilesApi.create(input),
    onSuccess: (profile) => {
      invalidate();
      toast.success("Profile created", { description: `${profile.commonName} added to the catalogue.` });
    },
    onError: (error) => toastError("Could not create profile", error),
  });

  const update = useMutation({
    mutationFn: ({ id, input }: { id: number; input: PlantProfileInput }) => plantProfilesApi.update(id, input),
    onSuccess: (profile) => {
      invalidate();
      toast.success("Profile updated", { description: `${profile.commonName} saved.` });
    },
    onError: (error) => toastError("Could not update profile", error),
  });

  return { create, update };
}
