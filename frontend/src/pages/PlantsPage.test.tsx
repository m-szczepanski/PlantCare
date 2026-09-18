import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { plantsApi } from "@/api/client";
import type { Plant } from "@/api/types";
import PlantsPage from "@/pages/PlantsPage";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  plantsApi: {
    list: vi.fn(),
    get: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
  },
  plantProfilesApi: { list: vi.fn().mockResolvedValue([]) },
  ApiError: class ApiError extends Error {},
}));

const monstera: Plant = {
  id: 1,
  nickName: "Monstera Mike",
  location: "Living room",
  photoUrl: null,
  acquiredDate: "2026-01-01T00:00:00",
  plantProfileId: 2,
  profileCommonName: "Monstera",
  customWateringIntervalDays: null,
  lastWateredAt: "2026-03-01T00:00:00",
  dueStatus: "Overdue",
  wateringIntervalDays: 7,
  daysUntilDue: -3,
  nextDueDate: "2026-03-08T00:00:00",
  dueMessage: "3 days overdue",
};

describe("PlantsPage", () => {
  beforeEach(() => {
    vi.mocked(plantsApi.list).mockReset();
  });

  it("renders the list of plants with due status", async () => {
    vi.mocked(plantsApi.list).mockResolvedValue([monstera]);

    renderWithProviders(<PlantsPage />);

    expect(await screen.findByText("Monstera Mike")).toBeInTheDocument();
    expect(screen.getByText("Living room")).toBeInTheDocument();
    expect(screen.getByText("3 days overdue")).toBeInTheDocument();
  });

  it("shows the empty state when there are no plants", async () => {
    vi.mocked(plantsApi.list).mockResolvedValue([]);

    renderWithProviders(<PlantsPage />);

    await waitFor(() =>
      expect(screen.getByText(/You have no plants yet/i)).toBeInTheDocument(),
    );
  });
});
