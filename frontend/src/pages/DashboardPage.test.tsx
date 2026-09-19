import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen, within } from "@testing-library/react";
import { dashboardApi } from "@/api/client";
import type { Dashboard, Plant } from "@/api/types";
import DashboardPage from "@/pages/DashboardPage";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  dashboardApi: { get: vi.fn() },
  ApiError: class ApiError extends Error {},
}));

function plant(over: Partial<Plant>): Plant {
  return {
    id: 1,
    nickName: "Plant",
    location: "Desk",
    photoUrl: null,
    acquiredDate: "2026-01-01T00:00:00",
    plantProfileId: null,
    profileCommonName: null,
    careTips: null,
    customWateringIntervalDays: null,
    lastWateredAt: null,
    dueStatus: "Upcoming",
    wateringIntervalDays: 7,
    daysUntilDue: 3,
    nextDueDate: "2026-03-22T00:00:00",
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

    expect(await screen.findByRole("heading", { name: "Overdue" })).toBeInTheDocument();
    expect(screen.getByText("Thirsty Theo")).toBeInTheDocument();
    expect(screen.getByText("Parched Paula")).toBeInTheDocument();
    expect(screen.getByText("Fine Fiona")).toBeInTheDocument();
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

  it("shows the empty state when there are no plants", async () => {
    vi.mocked(dashboardApi.get).mockResolvedValue(emptyDashboard);

    renderWithProviders(<DashboardPage />);

    expect(await screen.findByRole("heading", { name: "No plants yet" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Add your first plant" })).toBeInTheDocument();
  });
});
