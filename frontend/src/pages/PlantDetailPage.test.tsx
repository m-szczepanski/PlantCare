import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { plantsApi } from "@/api/client";
import type { Plant, WateringLogEntry } from "@/api/types";
import PlantDetailPage from "@/pages/PlantDetailPage";
import { renderWithProviders } from "@/test/render";

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
  ApiError: class ApiError extends Error {},
}));

const plant: Plant = {
  id: 1,
  nickName: "Monstera Mike",
  location: "Living room",
  photoUrl: null,
  acquiredDate: "2026-01-01T00:00:00",
  plantProfileId: null,
  profileCommonName: null,
  careTips: null,
  customWateringIntervalDays: 7,
  lastWateredAt: "2026-03-01T00:00:00",
  dueStatus: "Overdue",
  wateringIntervalDays: 7,
  daysUntilDue: -3,
  nextDueDate: "2026-03-08T00:00:00",
  dueMessage: "3 days overdue",
};

const logs: WateringLogEntry[] = [
  { id: 2, wateredAt: "2026-03-01T09:00:00", note: "Soaked thoroughly" },
  { id: 1, wateredAt: "2026-02-20T09:00:00", note: null },
];

describe("PlantDetailPage", () => {
  beforeEach(() => {
    vi.mocked(plantsApi.get).mockReset().mockResolvedValue(plant);
    vi.mocked(plantsApi.wateringLogs).mockReset().mockResolvedValue(logs);
    vi.mocked(plantsApi.water).mockReset().mockResolvedValue({
      ...plant,
      lastWateredAt: "2026-03-04T10:00:00",
      dueStatus: "Upcoming",
      daysUntilDue: 7,
      dueMessage: "7 days until due",
    });
  });

  it("renders the watering history from the log", async () => {
    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    expect(await screen.findByText("Soaked thoroughly")).toBeInTheDocument();
    expect(screen.getByText("Monstera Mike")).toBeInTheDocument();
  });

  it("shows an empty history state when nothing was logged", async () => {
    vi.mocked(plantsApi.wateringLogs).mockResolvedValue([]);

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    expect(await screen.findByText(/No waterings logged yet/i)).toBeInTheDocument();
  });

  it("marks the plant as watered through the API client", async () => {
    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    fireEvent.click(await screen.findByRole("button", { name: "Mark as watered" }));

    await waitFor(() => expect(plantsApi.water).toHaveBeenCalledWith(1, undefined));
    await waitFor(() => expect(plantsApi.wateringLogs).toHaveBeenCalledTimes(2));
  });

  it("renders care tips from the profile with markdown formatting", async () => {
    vi.mocked(plantsApi.get).mockResolvedValue({
      ...plant,
      profileCommonName: "Monstera",
      careTips: {
        commonName: "Monstera",
        lightRequirement: "Bright",
        humidityNotes: "Loves misting.",
        careTips: "Feed **monthly** in summer.",
      },
    });

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    expect(await screen.findByText(/Care tips — Monstera/)).toBeInTheDocument();
    expect(screen.getByText("Bright, indirect light")).toBeInTheDocument();
    expect(screen.getByText("Loves misting.")).toBeInTheDocument();
    expect(screen.getByText("monthly").closest("strong")).not.toBeNull();
  });

  it("hides the care tips section for plants without a profile", async () => {
    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    await screen.findByText("Monstera Mike");
    expect(screen.queryByText(/Care tips/)).not.toBeInTheDocument();
  });
});
