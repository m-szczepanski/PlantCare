import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { plantsApi } from "@/api/client";
import type { Plant } from "@/api/types";
import { useCreatePlant, usePlants } from "@/hooks/usePlants";

vi.mock("@/api/client", () => ({
  plantsApi: {
    list: vi.fn(),
    get: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
    water: vi.fn(),
    wateringLogs: vi.fn(),
  },
  plantProfilesApi: { list: vi.fn().mockResolvedValue([]) },
  ApiError: class ApiError extends Error {},
}));

const plant: Plant = {
  id: 1,
  nickName: "Pothos",
  location: "Shelf",
  photoUrl: null,
  acquiredDate: "2026-01-01T00:00:00",
  plantProfileId: null,
  profileCommonName: null,
  careTips: null,
  customWateringIntervalDays: 7,
  lastWateredAt: null,
  dueStatus: "Upcoming",
  wateringIntervalDays: 7,
  daysUntilDue: 7,
  nextDueDate: "2026-03-22T00:00:00",
  dueMessage: "7 days until due",
};

function wrapper({ children }: { children: ReactNode }) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}

describe("usePlants hooks", () => {
  beforeEach(() => {
    vi.mocked(plantsApi.list).mockReset();
    vi.mocked(plantsApi.create).mockReset();
  });

  it("usePlants resolves the fetched list", async () => {
    vi.mocked(plantsApi.list).mockResolvedValue([plant]);

    const { result } = renderHook(() => usePlants(), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toEqual([plant]);
  });

  it("useCreatePlant posts the input through the client", async () => {
    vi.mocked(plantsApi.create).mockResolvedValue(plant);

    const { result } = renderHook(() => useCreatePlant(), { wrapper });
    result.current.mutate({ nickName: "Pothos", location: "Shelf", acquiredDate: "2026-01-01T00:00:00" });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(plantsApi.create).toHaveBeenCalledTimes(1);
  });
});
