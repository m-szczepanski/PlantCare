import type { Plant, PlantInput, PlantProfileOption, WateringLogEntry } from "./types";

export interface HealthResponse {
  status: string;
}

const BASE_URL = "/api";

export class ApiError extends Error {
  readonly status: number;
  readonly detail?: string;

  constructor(status: number, message: string, detail?: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.detail = detail;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    headers: { "Content-Type": "application/json", ...(init?.headers ?? {}) },
    ...init,
  });

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  const body = text ? safeParse(text) : null;

  if (!response.ok) {
    const detail =
      (body as { detail?: string } | null)?.detail ??
      firstValidationMessage(body as { errors?: Record<string, string[]> } | null);
    throw new ApiError(response.status, `Request to ${path} failed (${response.status})`, detail);
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
  wateringLogs: (id: number) => request<WateringLogEntry[]>(`/plants/${id}/watering-logs`),
};

export const plantProfilesApi = {
  list: () => request<PlantProfileOption[]>("/plant-profiles"),
};

export const healthApi = {
  get: () => request<HealthResponse>("/health"),
};
