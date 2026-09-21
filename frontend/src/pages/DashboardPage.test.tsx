import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { dashboardApi } from "@/api/client";
import type { Dashboard, Plant } from "@/api/types";
import DashboardPage from "@/pages/DashboardPage";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  dashboardApi: { get: vi.fn() },
  healthApi: { status: vi.fn() },
  plantsApi: {
    list: vi.fn().mockResolvedValue([]),
    snoozeAll: vi.fn(),
    water: vi.fn(),
    bulkWater: vi.fn(),
  },
  ApiError: class ApiError extends Error {},
}));

function plant(over: Partial<Plant>): Plant {
  return {
    id: 1,
    nickName: "Plant",
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
    customWateringIntervalDays: null,
    reduceInWinter: null,
    lastWateredAt: null,
    dueStatus: "Upcoming",
    wateringIntervalDays: 7,
    daysUntilDue: 3,
    nextDueDate: "2026-03-22T00:00:00",
    roomLightMatch: null,
    dueMessage: "3 days until due",
    ...over,
  };
}

const emptyDashboard: Dashboard = { overdue: [], dueToday: [], upcoming: [] };

describe("DashboardPage", () => {
  beforeEach(() => {
    vi.mocked(dashboardApi.get).mockReset();
  });

  it("renders the three buckets with their plants", async () => {
    vi.mocked(dashboardApi.get).mockResolvedValue({
      overdue: [plant({ id: 1, nickName: "Thirsty Theo", dueStatus: "Overdue", dueMessage: "2 days overdue" })],
      dueToday: [plant({ id: 2, nickName: "Parched Paula", dueStatus: "DueToday", dueMessage: "Due today" })],
      upcoming: [plant({ id: 3, nickName: "Fine Fiona" })],
    });

    renderWithProviders(<DashboardPage />);

    expect(await screen.findByRole("heading", { level: 2, name: "Overdue" })).toBeInTheDocument();
    expect(screen.getByRole("group", { name: "Overdue: 1" })).toBeInTheDocument();
    expect(screen.getByRole("group", { name: "Due today: 1" })).toBeInTheDocument();
    expect(screen.getByRole("group", { name: "Plants: 3" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Thirsty Theo" })).toBeInTheDocument();
    expect(screen.getByText("Parched Paula")).toBeInTheDocument();
    expect(screen.getByText("Fine Fiona")).toBeInTheDocument();
  });

  it("waters a whole bucket in one batch call", async () => {
    const { plantsApi } = await import("@/api/client");
    vi.mocked(plantsApi.bulkWater).mockResolvedValue({ requested: 1, watered: 1, skippedIds: [] });
    vi.mocked(dashboardApi.get).mockResolvedValue({
      overdue: [plant({ id: 1, nickName: "Thirsty Theo", dueStatus: "Overdue", dueMessage: "2 days overdue" })],
      dueToday: [],
      upcoming: [],
    });

    renderWithProviders(<DashboardPage />);

    const waterAll = await screen.findByRole("button", { name: "Water all" });
    fireEvent.click(waterAll);

    await waitFor(() => expect(plantsApi.bulkWater).toHaveBeenCalledWith([1]));
  });

  it("snoozes every plant for the vacation length", async () => {
    const { plantsApi } = await import("@/api/client");
    vi.mocked(plantsApi.snoozeAll).mockResolvedValue({ snoozedPlants: 3 });
    vi.mocked(dashboardApi.get).mockResolvedValue({
      ...emptyDashboard,
      upcoming: [plant({ id: 3, nickName: "Fine Fiona" })],
    });

    renderWithProviders(<DashboardPage />);

    await screen.findByRole("heading", { level: 2, name: "Upcoming" });
    fireEvent.click(screen.getByRole("button", { name: "Snooze all" }));

    await waitFor(() => expect(plantsApi.snoozeAll).toHaveBeenCalledWith(14));
  });

  it("shows the all-caught-up state when nothing is overdue or due today", async () => {
    vi.mocked(dashboardApi.get).mockResolvedValue({
      ...emptyDashboard,
      upcoming: [plant({ id: 3, nickName: "Fine Fiona" })],
    });

    renderWithProviders(<DashboardPage />);

    await screen.findByText("Fine Fiona");
    expect(screen.getByRole("group", { name: "Overdue: 0" })).toBeInTheDocument();
    expect(screen.getByText(/All caught up/i)).toBeInTheDocument();
  });

  it("hides empty sections", async () => {
    vi.mocked(dashboardApi.get).mockResolvedValue({
      ...emptyDashboard,
      upcoming: [plant({ id: 3, nickName: "Fine Fiona" })],
    });

    renderWithProviders(<DashboardPage />);

    expect(await screen.findByRole("heading", { name: "Upcoming" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Overdue" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Due today" })).not.toBeInTheDocument();
  });

  it("shows card skeletons while the dashboard loads", () => {
    vi.mocked(dashboardApi.get).mockReturnValue(new Promise(() => {}));

    renderWithProviders(<DashboardPage />);

    const status = screen.getByRole("status");
    expect(within(status).getByText("Loading dashboard...")).toBeInTheDocument();
    expect(screen.queryByText("Overdue")).not.toBeInTheDocument();
  });

  it("groups plants by room when the room toggle is active", async () => {
    vi.mocked(dashboardApi.get).mockResolvedValue({
      ...emptyDashboard,
      upcoming: [
        plant({ id: 1, nickName: "Kitchen Kate", roomName: "Kitchen" }),
        plant({ id: 2, nickName: "Office Ollie", roomName: "Office" }),
      ],
    });

    renderWithProviders(<DashboardPage />, { route: "/?group=room" });

    expect(await screen.findByRole("heading", { level: 2, name: "Kitchen" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { level: 2, name: "Office" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { level: 2, name: "Upcoming" })).not.toBeInTheDocument();
  });

  it("shows the onboarding checklist with the ntfy subscribe link", async () => {
    const { healthApi } = await import("@/api/client");
    vi.mocked(healthApi.status).mockResolvedValue({
      nowUtc: "2026-09-20T07:00:00Z",
      timeZoneId: "UTC",
      wateringCheckCron: "0 8 * * *",
      lastJobRun: null,
      lastDigest: null,
      ntfy: {
        baseUrl: "http://ntfy:80",
        topic: "plant-care",
        subscribeUrl: "http://ntfy:80/plant-care",
        reachable: true,
        latencyMs: 8,
        error: null,
      },
    });
    vi.mocked(dashboardApi.get).mockResolvedValue(emptyDashboard);

    renderWithProviders(<DashboardPage />);

    expect(await screen.findByText("Getting started")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "plant-care" })).toHaveAttribute(
      "href",
      "http://ntfy:80/plant-care",
    );
  });

  it("shows the empty state when there are no plants", async () => {
    vi.mocked(dashboardApi.get).mockResolvedValue(emptyDashboard);

    renderWithProviders(<DashboardPage />);

    expect(await screen.findByRole("heading", { name: "No plants yet" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Add your first plant" })).toBeInTheDocument();
  });
});
