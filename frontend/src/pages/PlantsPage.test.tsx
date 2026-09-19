import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, within } from "@testing-library/react";
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
  roomId: null,

  roomName: "Living room",
  photoUrl: null,
  acquiredDate: "2026-01-01T00:00:00",
  plantProfileId: 2,
  profileCommonName: "Monstera",
  careTips: null,
  customWateringIntervalDays: null,
  lastWateredAt: new Date(Date.now() - 3 * 86_400_000).toISOString(),
  dueStatus: "Overdue",
  wateringIntervalDays: 7,
  daysUntilDue: -3,
  nextDueDate: "2026-03-08T00:00:00",
  dueMessage: "3 days overdue",
  roomLightMatch: null,
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
    expect(screen.getByText("3")).toBeInTheDocument();
    expect(screen.getByText("days overdue")).toBeInTheDocument();
    expect(screen.getByText("Overdue")).toBeInTheDocument();
    expect(screen.getByText("Watered 3 days ago")).toBeInTheDocument();
    expect(screen.getByRole("img", { name: "No photo of Monstera Mike" })).toBeInTheDocument();
  });

  it("shows card skeletons while the list loads", () => {
    vi.mocked(plantsApi.list).mockReturnValue(new Promise(() => {}));

    renderWithProviders(<PlantsPage />);

    const status = screen.getByRole("status");
    expect(within(status).getByText("Loading plants...")).toBeInTheDocument();
  });

  it("shows the empty state when there are no plants", async () => {
    vi.mocked(plantsApi.list).mockResolvedValue([]);

    renderWithProviders(<PlantsPage />);

    await screen.findByRole("heading", { name: "No plants yet" });
    expect(screen.getByRole("link", { name: "Add your first plant" })).toBeInTheDocument();
  });

  it("filters, shows a no-results state, and clears via the toolbar", async () => {
    vi.mocked(plantsApi.list).mockResolvedValue([
      monstera,
      { ...monstera, id: 2, nickName: "Golden Pothos", roomId: null,
 roomName: "Bathroom", profileCommonName: "Pothos" },
    ]);

    renderWithProviders(<PlantsPage />);

    await screen.findByText("Monstera Mike");
    expect(screen.getByText("2 of 2")).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("Search plants"), { target: { value: "pothos" } });
    expect(await screen.findByText("Golden Pothos")).toBeInTheDocument();
    expect(screen.queryByText("Monstera Mike")).not.toBeInTheDocument();
    expect(screen.getByText("1 of 2")).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("Search plants"), { target: { value: "zzz" } });
    expect(await screen.findByRole("heading", { name: "No matching plants" })).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Clear filters" }));
    expect(await screen.findByText("Monstera Mike")).toBeInTheDocument();
    expect(screen.getByText("2 of 2")).toBeInTheDocument();
  });

  it("restores filters from the URL for shareable deep links", async () => {
    vi.mocked(plantsApi.list).mockResolvedValue([
      monstera,
      { ...monstera, id: 2, nickName: "Golden Pothos", roomId: null,
 roomName: "Bathroom", profileCommonName: "Pothos" },
    ]);

    renderWithProviders(<PlantsPage />, { route: "/plants?q=Poth" });

    const searchInput = await screen.findByLabelText("Search plants");
    expect(searchInput).toHaveValue("Poth");
    expect(await screen.findByText("Golden Pothos")).toBeInTheDocument();
    expect(screen.queryByText("Monstera Mike")).not.toBeInTheDocument();
  });
});
