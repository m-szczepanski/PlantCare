import type { Dashboard, Insights, Plant, PlantInput, PlantProfileOption, WateringLogEntry } from "./types";

export interface HealthResponse {
  status: string;
}

const BASE_URL = "/api";

export class ApiError extends Error {
  readonly status: number;
  readonly detail?: string;
  readonly errors?: Record<string, string[]>;

  constructor(
    status: number,
    message: string,
    detail?: string,
    errors?: Record<string, string[]>,
  ) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.detail = detail;
    this.errors = errors;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...(init?.headers ?? {}) },
  });
  return unwrap<T>(response, path);
}

async function unwrap<T>(response: Response, path: string): Promise<T> {
  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  const body = text ? safeParse(text) : null;

  if (!response.ok) {
    const errorBody = body as { detail?: string; errors?: Record<string, string[]> } | null;
    const detail = errorBody?.detail ?? firstValidationMessage(errorBody);
    throw new ApiError(
      response.status,
      `Request to ${path} failed (${response.status})`,
      detail,
      errorBody?.errors,
    );
  }

  return body as T;
}

function safeParse(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

function firstValidationMessage(body: { errors?: Record<string, string[]> } | null): string | undefined {
  const errors = body?.errors;
  if (!errors) return undefined;
  const first = Object.values(errors).flat()[0];
  return first;
}

export const plantsApi = {
  list: () => request<Plant[]>("/plants"),
  get: (id: number) => request<Plant>(`/plants/${id}`),
  create: (input: PlantInput) =>
    request<Plant>("/plants", { method: "POST", body: JSON.stringify(input) }),
  update: (id: number, input: PlantInput) =>
    request<Plant>(`/plants/${id}`, { method: "PUT", body: JSON.stringify(input) }),
  remove: (id: number) => request<void>(`/plants/${id}`, { method: "DELETE" }),
  water: (id: number, note?: string) =>
    request<Plant>(`/plants/${id}/water`, {
      method: "POST",
      body: JSON.stringify(note ? { note } : {}),
    }),
  undoWater: (id: number) => request<Plant>(`/plants/${id}/water`, { method: "DELETE" }),
  uploadPhoto: async (id: number, file: File): Promise<Plant> => {
    const form = new FormData();
    form.append("file", file);
    const response = await fetch(`${BASE_URL}/plants/${id}/photo`, { method: "POST", body: form });
    return unwrap<Plant>(response, `/plants/${id}/photo`);
  },
  wateringLogs: (id: number) => request<WateringLogEntry[]>(`/plants/${id}/watering-logs`),
};

export const plantProfilesApi = {
  list: () => request<PlantProfileOption[]>("/plant-profiles"),
};

export const dashboardApi = {
  get: () => request<Dashboard>("/dashboard"),
};

export const insightsApi = {
  get: () => request<Insights>("/insights"),
};

export const healthApi = {
  get: () => request<HealthResponse>("/health"),
};
