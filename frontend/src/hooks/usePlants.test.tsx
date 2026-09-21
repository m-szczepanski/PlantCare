import { beforeEach, describe, expect, it, vi } from "vitest";
import { act, renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { toast } from "sonner";
import { plantsApi } from "@/api/client";
import type { Plant } from "@/api/types";
import { plantKeys, useCreatePlant, usePlants, useWaterPlant } from "@/hooks/usePlants";

vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

vi.mock("@/api/client", () => ({
  plantsApi: {
    list: vi.fn(),
    get: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
    water: vi.fn(),
    undoWater: vi.fn(),
    wateringLogs: vi.fn(),
  },
  plantProfilesApi: { list: vi.fn().mockResolvedValue([]) },
  ApiError: class ApiError extends Error {},
}));

const plant: Plant = {
  id: 1,
  nickName: "Pothos",
  roomId: null,

  roomName: "Shelf",
  photoUrl: null,
  potSizeCm: null,
  soilType: null,
  soilMix: null,
  propagatedFrom: null,
  notifyEnabled: true,
  snoozedUntil: null,
  acquiredDate: "2026-01-01T00:00:00",
  plantProfileId: null,
  profileCommonName: null,
  profileToxicToPets: false,
  profileToxicToChildren: false,
  careTips: null,
  customWateringIntervalDays: 7,
  reduceInWinter: null,
  lastWateredAt: null,
  dueStatus: "Upcoming",
  wateringIntervalDays: 7,
  daysUntilDue: 7,
  nextDueDate: "2026-03-22T00:00:00",
  dueMessage: "7 days until due",
  roomLightMatch: null,
};

function wrapper({ children }: { children: ReactNode }) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}

function createWrapper() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const clientWrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  return { wrapper: clientWrapper, queryClient };
}

describe("usePlants hooks", () => {
  beforeEach(() => {
    vi.mocked(plantsApi.list).mockReset();
    vi.mocked(plantsApi.create).mockReset();
    vi.mocked(plantsApi.water).mockReset();
    vi.mocked(plantsApi.undoWater).mockReset();
    vi.mocked(toast.success).mockReset();
    vi.mocked(toast.error).mockReset();
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
    result.current.mutate({ nickName: "Pothos", roomId: null, acquiredDate: "2026-01-01T00:00:00" });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(plantsApi.create).toHaveBeenCalledTimes(1);
  });

  it("useCreatePlant shows a success toast", async () => {
    vi.mocked(plantsApi.create).mockResolvedValue(plant);

    const { result } = renderHook(() => useCreatePlant(), { wrapper });
    result.current.mutate({ nickName: "Pothos", roomId: null, acquiredDate: "2026-01-01T00:00:00" });

    await waitFor(() => expect(toast.success).toHaveBeenCalledWith("Plant added", {
      description: "Pothos is on the list.",
    }));
  });

  it("useWaterPlant shows a failure toast with the error message", async () => {
    vi.mocked(plantsApi.water).mockRejectedValue(new Error("boom"));

    const { result } = renderHook(() => useWaterPlant(), { wrapper });
    result.current.mutate({ id: 1 });

    await waitFor(() => expect(toast.error).toHaveBeenCalledWith("Could not log watering", {
      description: "boom",
    }));
  });

  it("useWaterPlant patches the cached plant before the server responds", async () => {
    const { wrapper: w, queryClient } = createWrapper();
    queryClient.setQueryData(plantKeys.detail(1), plant);
    let resolveWater: (p: Plant) => void = () => {};
    vi.mocked(plantsApi.water).mockReturnValue(
      new Promise<Plant>((resolve) => {
        resolveWater = resolve;
      }),
    );

    const { result } = renderHook(() => useWaterPlant(), { wrapper: w });
    act(() => result.current.mutate({ id: 1 }));

    const optimistic = queryClient.getQueryData<Plant>(plantKeys.detail(1));
    expect(optimistic?.dueStatus).toBe("Upcoming");
    expect(optimistic?.daysUntilDue).toBe(7);
    expect(new Date(optimistic!.lastWateredAt!).getTime()).toBeGreaterThan(Date.now() - 5000);

    act(() => resolveWater(plant));
  });

  it("useWaterPlant rolls the cache back when the request fails", async () => {
    const { wrapper: w, queryClient } = createWrapper();
    queryClient.setQueryData(plantKeys.detail(1), plant);
    vi.mocked(plantsApi.water).mockRejectedValue(new Error("boom"));

    const { result } = renderHook(() => useWaterPlant(), { wrapper: w });
    act(() => result.current.mutate({ id: 1 }));

    await waitFor(() => expect(toast.error).toHaveBeenCalled());
    expect(queryClient.getQueryData<Plant>(plantKeys.detail(1))).toEqual(plant);
  });

  it("the success toast offers an undo action that calls the API", async () => {
    const { wrapper: w } = createWrapper();
    vi.mocked(plantsApi.water).mockResolvedValue({ ...plant, lastWateredAt: new Date().toISOString() });
    vi.mocked(plantsApi.undoWater).mockResolvedValue(plant);

    const { result } = renderHook(() => useWaterPlant(), { wrapper: w });
    act(() => result.current.mutate({ id: 1 }));

    await waitFor(() => expect(toast.success).toHaveBeenCalled());
    const options = vi.mocked(toast.success).mock.calls.at(-1)?.[1] as
      | { action?: { label: string; onClick: () => void } }
      | undefined;
    expect(options?.action?.label).toBe("Undo");

    act(() => options?.action?.onClick());
    await waitFor(() => expect(plantsApi.undoWater).toHaveBeenCalledWith(1));
    await waitFor(() =>
      expect(toast.success).toHaveBeenCalledWith("Watering undone", { description: "Pothos restored." }),
    );
  });
});
