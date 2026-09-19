import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { insightsApi } from "@/api/client";
import type { Insights } from "@/api/types";
import InsightsPage from "@/pages/InsightsPage";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  insightsApi: { get: vi.fn() },
  ApiError: class ApiError extends Error {},
}));

const currentMonth = new Date().toISOString().slice(0, 7);

const insights: Insights = {
  totalPlants: 4,
  scheduledPlants: 3,
  unscheduledPlants: 1,
  speciesCount: 2,
  species: [
    { plantProfileId: 1, commonName: "Monstera", plantCount: 2 },
    { plantProfileId: 2, commonName: "Pothos", plantCount: 1 },
  ],
  mostNeglected: [
    { plantId: 3, nickName: "Ignored Ivy", daysSinceLastWatering: 40 },
    { plantId: 1, nickName: "Monstera Mike", daysSinceLastWatering: 3 },
  ],
  adherenceWindowDays: 30,
  adherencePercent: 66.7,
  expectedWateringsInWindow: 9,
  actualWateringsInWindow: 6,
  streaks: [{ plantId: 1, nickName: "Monstera Mike", consecutiveOnTimeWaterings: 5 }],
  monthlyWaterings: [
    { month: "2026-02", count: 0 },
    { month: "2026-03", count: 2 },
    { month: currentMonth, count: 6 },
  ],
};

beforeEach(() => {
  vi.mocked(insightsApi.get).mockReset().mockResolvedValue(insights);
});

describe("InsightsPage", () => {
  it("renders summary tiles", async () => {
    renderWithProviders(<InsightsPage />, { route: "/insights" });

    expect(await screen.findByRole("group", { name: "Plants: 4" })).toBeInTheDocument();
    expect(screen.getByRole("group", { name: "Scheduled: 3" })).toBeInTheDocument();
    expect(screen.getByRole("group", { name: "Species: 2" })).toBeInTheDocument();
    expect(screen.getByText(/67%/)).toBeInTheDocument();
    expect(screen.getByText(/6\/9 waterings/)).toBeInTheDocument();
  });

  it("renders the diversity, neglect and streak lists", async () => {
    renderWithProviders(<InsightsPage />, { route: "/insights" });

    expect(await screen.findByText(/Ignored Ivy/)).toBeInTheDocument();
    expect(screen.getByText("40d ago")).toBeInTheDocument();
    expect(screen.getByText("Monstera")).toBeInTheDocument();
    expect(screen.getByText("×2")).toBeInTheDocument();
    expect(screen.getByText("5 on time")).toBeInTheDocument();
  });

  it("renders the monthly watering chart with an sr-only table", async () => {
    renderWithProviders(<InsightsPage />, { route: "/insights" });

    expect(await screen.findByText("Waterings per month")).toBeInTheDocument();
    const rows = screen.getAllByRole("row");
    expect(rows).toHaveLength(4);
    expect(rows[3]).toHaveTextContent(currentMonth);
    expect(rows[3]).toHaveTextContent("6");
  });
});
