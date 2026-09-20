import type { CareTask, CareTaskType, Dashboard, Insights, JournalEntry, Plant, PlantInput, PlantNote, PlantProfile, PlantProfileInput, PlantProfileOption, Room, RoomInput, WaterDetails, WateringLogEntry } from "./types";

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
  water: (id: number, details?: WaterDetails) =>
    request<Plant>(`/plants/${id}/water`, {
      method: "POST",
      body: JSON.stringify(details ?? {}),
    }),
  undoWater: (id: number) => request<Plant>(`/plants/${id}/water`, { method: "DELETE" }),
  bulkWater: (ids: number[]) =>
    request<{ requested: number; watered: number; skippedIds: number[] }>("/plants/bulk-water", {
      method: "POST",
      body: JSON.stringify({ ids }),
    }),
  uploadPhoto: async (id: number, file: File): Promise<Plant> => {
    const form = new FormData();
    form.append("file", file);
    const response = await fetch(`${BASE_URL}/plants/${id}/photo`, { method: "POST", body: form });
    return unwrap<Plant>(response, `/plants/${id}/photo`);
  },
  wateringLogs: (id: number) => request<WateringLogEntry[]>(`/plants/${id}/watering-logs`),
  careTasks: (id: number) => request<CareTask[]>(`/plants/${id}/care-tasks`),
  addCareTask: (id: number, input: { type: CareTaskType; intervalDays: number; reduceInWinter?: boolean }) =>
    request<CareTask>(`/plants/${id}/care-tasks`, { method: "POST", body: JSON.stringify(input) }),
  deleteCareTask: (id: number, taskId: number) =>
    request<void>(`/plants/${id}/care-tasks/${taskId}`, { method: "DELETE" }),
  markCareTaskDone: (id: number, type: CareTaskType) =>
    request<CareTask>(`/plants/${id}/care-tasks/${type}/done`, { method: "POST", body: JSON.stringify({}) }),
  snooze: (id: number, days: number) =>
    request<Plant>(`/plants/${id}/snooze`, { method: "POST", body: JSON.stringify({ days }) }),
  clearSnooze: (id: number) => request<Plant>(`/plants/${id}/snooze`, { method: "DELETE" }),
  snoozeAll: (days: number) =>
    request<{ snoozedPlants: number }>(`/plants/snooze-all`, { method: "POST", body: JSON.stringify({ days }) }),
  journal: (id: number) => request<JournalEntry[]>(`/plants/${id}/journal`),
  addJournalEntry: async (id: number, input: { entryDate?: string; text?: string; file?: File | null }): Promise<JournalEntry> => {
    const form = new FormData();
    if (input.entryDate) form.append("entryDate", input.entryDate);
    if (input.text) form.append("text", input.text);
    if (input.file) form.append("file", input.file);
    const response = await fetch(`${BASE_URL}/plants/${id}/journal`, { method: "POST", body: form });
    return unwrap<JournalEntry>(response, `/plants/${id}/journal`);
  },
  deleteJournalEntry: (id: number, entryId: number) =>
    request<void>(`/plants/${id}/journal/${entryId}`, { method: "DELETE" }),
  notes: (id: number) => request<PlantNote[]>(`/plants/${id}/notes`),
  addNote: (id: number, text: string) =>
    request<PlantNote>(`/plants/${id}/notes`, { method: "POST", body: JSON.stringify({ text }) }),
};

export const plantProfilesApi = {
  list: () => request<PlantProfile[]>("/plant-profiles"),
  create: (input: PlantProfileInput) =>
    request<PlantProfile>("/plant-profiles", { method: "POST", body: JSON.stringify(input) }),
  update: (id: number, input: PlantProfileInput) =>
    request<PlantProfile>(`/plant-profiles/${id}`, { method: "PUT", body: JSON.stringify(input) }),
};

export const roomsApi = {
  list: () => request<Room[]>("/rooms"),
  create: (input: RoomInput) =>
    request<Room>("/rooms", { method: "POST", body: JSON.stringify(input) }),
  update: (id: number, input: RoomInput) =>
    request<Room>(`/rooms/${id}`, { method: "PUT", body: JSON.stringify(input) }),
  remove: (id: number) => request<void>(`/rooms/${id}`, { method: "DELETE" }),
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
