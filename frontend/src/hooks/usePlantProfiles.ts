import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { plantProfilesApi } from "@/api/client";
import type { PlantProfileInput } from "@/api/types";
import { toastError } from "@/lib/toast";
import { dashboardKeys } from "@/hooks/useDashboard";
import { plantKeys } from "@/hooks/usePlants";
import i18n from "@/i18n";

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
      toast.success(i18n.t("toasts.profileCreated"), { description: i18n.t("toasts.profileCreatedDesc", { name: profile.commonName }) });
    },
    onError: (error) => toastError(i18n.t("toasts.profileCreateFailed"), error),
  });

  const update = useMutation({
    mutationFn: ({ id, input }: { id: number; input: PlantProfileInput }) => plantProfilesApi.update(id, input),
    onSuccess: (profile) => {
      invalidate();
      queryClient.invalidateQueries({ queryKey: plantKeys.all });
      queryClient.invalidateQueries({ queryKey: dashboardKeys.all });
      toast.success(i18n.t("toasts.profileUpdated"), { description: i18n.t("toasts.profileSavedDesc", { name: profile.commonName }) });
    },
    onError: (error) => toastError(i18n.t("toasts.profileUpdateFailed"), error),
  });

  return { create, update };
}
