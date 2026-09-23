import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { dashboardApi } from "@/api/client";
import type { Dashboard, Plant } from "@/api/types";
import WallPage from "@/pages/WallPage";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  dashboardApi: { get: vi.fn() },
  ApiError: class ApiError extends Error {},
}));

function plant(over: Partial<Plant>): Plant {
  return {
    id: 1,
    nickName: "Thirsty Theo",
    roomId: null,

    roomName: "Desk",
    photoUrl: null,
    potSizeCm: null,
    soilType: null,
    soilMix: null,
    propagatedFrom: null,
    notifyEnabled: true,
    snoozedUntil: null,
    soilWetUntil: null,
    acquiredDate: "2026-01-01T00:00:00",
    plantProfileId: null,
    profileCommonName: null,
    profileToxicToPets: false,
    profileToxicToChildren: false,
    careTips: null,
    customWateringIntervalDays: 7,
    reduceInWinter: null,
    lastWateredAt: null,
    dueStatus: "Overdue",
    wateringIntervalDays: 7,
    daysUntilDue: -2,
    nextDueDate: "2026-03-20T00:00:00",
    dueMessage: "2 days overdue",
    roomLightMatch: null,
    healthStatus: null,
    lastCheckupAt: null,
    checkupDue: false,
    ...over,
  };
}

const dashboard: Dashboard = {
  overdue: [plant({})],
  dueToday: [],
  upcoming: [plant({ id: 2, nickName: "Fine Fiona", dueStatus: "Upcoming", daysUntilDue: 4 })],
};

beforeEach(() => {
  vi.mocked(dashboardApi.get).mockReset().mockResolvedValue(dashboard);
});

describe("WallPage", () => {
  it("renders a read-only dashboard with stats and buckets", async () => {
    renderWithProviders(<WallPage />, { route: "/wall" });

    expect(await screen.findByRole("heading", { level: 2, name: "Overdue" })).toBeInTheDocument();
    expect(screen.getByRole("group", { name: "Overdue: 1" })).toBeInTheDocument();
    expect(screen.getByText("Thirsty Theo")).toBeInTheDocument();
    expect(screen.getByText("Fine Fiona")).toBeInTheDocument();
    expect(screen.getByRole("time")).toBeInTheDocument();
  });

  it("shows no interactive controls", async () => {
    renderWithProviders(<WallPage />, { route: "/wall" });

    await screen.findByText("Thirsty Theo");
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Add plant" })).not.toBeInTheDocument();
  });
});
