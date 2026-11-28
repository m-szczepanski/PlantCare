import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen } from "@testing-library/react";
import { plantsApi } from "@/api/client";
import type { Plant } from "@/api/types";
import CalendarPage, { weekdayLabels } from "@/pages/CalendarPage";
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
    potSizeCm: null,
    soilType: null,
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
    dueStatus: "Upcoming",
    wateringIntervalDays: 7,
    daysUntilDue: 3,
    nextDueDate: `${localKey(3)}T00:00:00`,
    dueMessage: "",
    roomLightMatch: null,
    healthStatus: null,
    lastCheckupAt: null,
    checkupDue: false,
    topUpWateringEnabled: false,
    topUpWateringStatus: "NotScheduled",
    topUpWateringDaysUntilDue: null,
    topUpWateringNextDueDate: null,
    topUpWateringMessage: null,
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

  it("stacks days into a single-column list below the sm breakpoint", async () => {
    renderWithProviders(<CalendarPage />, { route: "/calendar" });

    const cell = await screen.findByLabelText(/Monstera Mike/);
    const grid = cell.parentElement!;
    expect(grid).toHaveClass("grid", "sm:grid-cols-7");
    expect(grid).not.toHaveClass("grid-cols-7");
    expect(cell).toHaveClass("sm:min-h-24");
    expect(cell).not.toHaveClass("min-h-24");
  });

  it("wraps the month toolbar and enlarges the chevrons on phones", async () => {
    renderWithProviders(<CalendarPage />, { route: "/calendar" });

    fireEvent.click(await screen.findByRole("button", { name: "Month" }));

    const next = screen.getByRole("button", { name: "Next month" });
    expect(next).toHaveClass("h-11", "sm:h-9");
    expect(next.parentElement).toHaveClass("flex-wrap");
  });

  it("labels the weekday columns Monday through Sunday instead of repeating one day", () => {
    expect(weekdayLabels("en")).toEqual(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"]);

    const polish = weekdayLabels("pl");
    expect(polish).toHaveLength(7);
    expect(new Set(polish).size).toBe(7);
    expect(polish[0]).toBe("pon.");
  });

  it("shows weekday headers matching the upcoming days in week view", async () => {
    const { container } = renderWithProviders(<CalendarPage />, { route: "/calendar" });
    await screen.findByLabelText(/Monstera Mike/);

    const header = container.querySelector(".grid-cols-7");
    expect(header).not.toBeNull();
    const expected = Array.from({ length: 7 }, (_, index) => {
      const date = new Date();
      date.setDate(date.getDate() + index);
      return date.toLocaleDateString("en", { weekday: "short" });
    });
    expect(Array.from(header!.children).map((child) => child.textContent)).toEqual(expected);
  });

  it("shows Monday-first weekday headers in month view", async () => {
    const { container } = renderWithProviders(<CalendarPage />, { route: "/calendar" });
    fireEvent.click(await screen.findByRole("button", { name: "Month" }));

    const header = container.querySelector(".grid-cols-7");
    expect(header).not.toBeNull();
    expect(Array.from(header!.children).map((child) => child.textContent)).toEqual(weekdayLabels("en"));
  });
});
