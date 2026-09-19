import { ApiError } from "@/api/client";

export interface SplitApiError {
  banner: string | null;
  fields: Record<string, string>;
}

// ASP.NET model-validation ProblemDetails use PascalCase keys ("NickName");
// the form fields are camelCase.
function toCamel(key: string): string {
  return key.charAt(0).toLowerCase() + key.slice(1);
}

export function splitApiError(error: unknown): SplitApiError {
  if (error instanceof ApiError && error.errors) {
    const fields: Record<string, string> = {};
    for (const [key, messages] of Object.entries(error.errors)) {
      const field = toCamel(key);
      if (messages.length > 0 && !(field in fields)) {
        fields[field] = messages[0];
      }
    }
    if (Object.keys(fields).length > 0) {
      return { banner: null, fields };
    }
  }

  if (!error) {
    return { banner: null, fields: {} };
  }

  return {
    banner: error instanceof ApiError && error.detail ? error.detail : (error as Error).message,
    fields: {},
  };
}
