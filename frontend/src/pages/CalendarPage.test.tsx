import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen } from "@testing-library/react";
import { plantsApi } from "@/api/client";
import type { Plant } from "@/api/types";
import CalendarPage from "@/pages/CalendarPage";
import { renderWithProviders } from "@/test/render";

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
    uploadPhoto: vi.fn(),
  },
  plantProfilesApi: { list: vi.fn().mockResolvedValue([]) },
  ApiError: class ApiError extends Error {},
}));

function localKey(offsetDays = 0): string {
  const date = new Date();
  date.setDate(date.getDate() + offsetDays);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
}

function plant(over: Partial<Plant>): Plant {
  return {
    id: 1,
    nickName: "Monstera Mike",
    roomId: null,

    roomName: "Living room",
    photoUrl: null,
    acquiredDate: "2026-01-01T00:00:00",
    plantProfileId: null,
    profileCommonName: null,
    careTips: null,
    customWateringIntervalDays: 7,
    lastWateredAt: null,
    dueStatus: "Upcoming",
    wateringIntervalDays: 7,
    daysUntilDue: 3,
    nextDueDate: `${localKey(3)}T00:00:00`,
    dueMessage: "",
    roomLightMatch: null,
    ...over,
  };
}

beforeEach(() => {
  vi.mocked(plantsApi.list).mockReset().mockResolvedValue([plant({})]);
});

describe("CalendarPage", () => {
  it("shows the week view with the plant on its due day", async () => {
    renderWithProviders(<CalendarPage />, { route: "/calendar" });

    await screen.findByRole("heading", { level: 1, name: "Calendar" });
    expect(await screen.findByLabelText(/Monstera Mike/)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Week" })).toHaveAttribute("aria-pressed", "true");
  });

  it("switches to the month view", async () => {
    renderWithProviders(<CalendarPage />, { route: "/calendar" });

    fireEvent.click(await screen.findByRole("button", { name: "Month" }));

    expect(screen.getByRole("button", { name: "Month" })).toHaveAttribute("aria-pressed", "true");
    expect(
      screen.getByText(new Date().toLocaleDateString(undefined, { month: "long", year: "numeric" })),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Next month" })).toBeInTheDocument();
  });

  it("renders breadcrumbs", async () => {
    renderWithProviders(<CalendarPage />, { route: "/calendar" });

    const nav = await screen.findByRole("navigation", { name: "Breadcrumb" });
    expect(nav).toBeInTheDocument();
  });
});
