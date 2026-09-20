import { toast } from "sonner";
import { ApiError } from "@/api/client";
import i18n from "@/i18n";

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError && error.detail) {
    return error.detail;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return i18n.t("common.unknownError");
}

export function toastError(title: string, error: unknown): void {
  toast.error(title, { description: errorMessage(error) });
}
