import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import i18n from "@/i18n";
import { notificationsApi } from "@/api/client";
import type { NotificationTestResult, WateringCheckResult } from "@/api/types";
import { toastError } from "@/lib/toast";

function describeTestResult(result: NotificationTestResult): string {
  return result.channels
    .map((c) => `${c.name}: ${c.delivered ? i18n.t("status.testDelivered") : i18n.t("status.testFailed")}`)
    .join(" · ");
}

export function useSendTestNotification() {
  return useMutation({
    mutationFn: (message?: string) => notificationsApi.sendTest(message),
    onSuccess: (result) => {
      const description = describeTestResult(result);
      if (result.channels.length === 0) {
        toast.warning(i18n.t("status.testNoChannels"));
      } else if (!result.anyDelivered) {
        toast.error(i18n.t("status.testFailedAll"), { description });
      } else if (result.delivered < result.total) {
        toast.warning(i18n.t("status.testPartial"), { description });
      } else {
        toast.success(i18n.t("status.testSent"), { description });
      }
    },
    onError: (error) => toastError(i18n.t("status.testError"), error),
  });
}

export function useRunWateringCheck() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => notificationsApi.runCheck(),
    onSuccess: (result: WateringCheckResult) => {
      queryClient.invalidateQueries({ queryKey: ["status"] });
      if (result.sentDigests > 0) {
        toast.success(i18n.t("status.checkSent"), {
          description: i18n.t("status.checkSentDesc", { count: result.plantsInDigest }),
        });
      } else if (result.failed > 0) {
        toast.error(i18n.t("status.checkFailed"));
      } else if (result.skippedDuplicates > 0) {
        toast.info(i18n.t("status.checkSkipped"));
      } else {
        toast.info(i18n.t("status.checkNothingDue"));
      }
    },
    onError: (error) => toastError(i18n.t("status.checkError"), error),
  });
}
