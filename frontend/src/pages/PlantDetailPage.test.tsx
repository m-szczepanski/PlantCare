import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { ApiError, plantsApi } from "@/api/client";
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
    uploadPhoto: vi.fn(),
  },
  ApiError: class ApiError extends Error {},
}));

const plant: Plant = {
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
  lastWateredAt: "2026-03-01T00:00:00",
  dueStatus: "Overdue",
  wateringIntervalDays: 7,
  daysUntilDue: -3,
  nextDueDate: "2026-03-08T00:00:00",
  dueMessage: "3 days overdue",
  roomLightMatch: null,
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
    expect(screen.getByRole("heading", { name: "Monstera Mike", level: 1 })).toBeInTheDocument();
    const breadcrumb = screen.getByRole("navigation", { name: "Breadcrumb" });
    expect(within(breadcrumb).getByRole("link", { name: "Plants" })).toBeInTheDocument();
    expect(within(breadcrumb).getByText("Monstera Mike")).toHaveAttribute("aria-current", "page");
  });

  it("shows a card-shaped skeleton while the plant loads", () => {
    vi.mocked(plantsApi.get).mockReturnValue(new Promise(() => {}));

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    const status = screen.getByRole("status");
    expect(within(status).getByText("Loading plant...")).toBeInTheDocument();
  });

  it("shows a not-found empty state for missing plants", async () => {
    vi.mocked(plantsApi.get).mockRejectedValue(
      Object.assign(new ApiError(404, "missing"), { status: 404 }),
    );

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    expect(await screen.findByRole("heading", { name: "Plant not found" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Back to plants" })).toBeInTheDocument();
  });

  it("shows an empty history state when nothing was logged", async () => {
    vi.mocked(plantsApi.wateringLogs).mockResolvedValue([]);

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    expect(
      await screen.findByRole("heading", { name: "No waterings logged yet" }),
    ).toBeInTheDocument();
  });

  it("renders the plant photo block from the profile photo url", async () => {
    vi.mocked(plantsApi.get).mockResolvedValue({
      ...plant,
      photoUrl: "https://example.com/mike.jpg",
    });

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    const img = await screen.findByRole("img", { name: "Monstera Mike" });
    expect(img).toHaveAttribute("src", "https://example.com/mike.jpg");
  });

  it("flags a wrong room with the light-match badge", async () => {
    vi.mocked(plantsApi.get).mockResolvedValue({
      ...plant,
      roomName: "Hallway",
      roomLightMatch: "MuchTooDark",
    });

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    expect(await screen.findByText("Wrong room — too dark")).toBeInTheDocument();
  });

  it("uploads a picked photo and shows it on the plant", async () => {
    vi.mocked(plantsApi.uploadPhoto).mockImplementation(async () => {
      const updated = { ...plant, photoUrl: "/uploads/plants/1/new.png" };
      vi.mocked(plantsApi.get).mockResolvedValue(updated);
      return updated;
    });

    const { container } = renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    await screen.findByRole("heading", { name: "Monstera Mike" });
    const fileInput = container.querySelector<HTMLInputElement>('input[type="file"]');
    expect(fileInput).not.toBeNull();

    fireEvent.change(fileInput!, {
      target: { files: [new File(["data"], "new.png", { type: "image/png" })] },
    });

    await waitFor(() => expect(plantsApi.uploadPhoto).toHaveBeenCalledWith(1, expect.any(File)));
    const img = await screen.findByRole("img", { name: "Monstera Mike" });
    expect(img).toHaveAttribute("src", "/uploads/plants/1/new.png");
  });

  it("gives the phone action buttons touch-friendly targets", async () => {
    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    const button = await screen.findByRole("button", { name: "Mark as watered" });
    expect(button).toHaveClass("min-h-11", "sm:min-h-9");
  });

  it("confirms deletion in a dialog", async () => {
    const remove = vi.fn().mockResolvedValue(undefined);
    vi.mocked(plantsApi.remove).mockImplementation(remove);

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    fireEvent.click(await screen.findByRole("button", { name: "Delete" }));
    const dialog = await screen.findByRole("alertdialog");
    expect(dialog).toHaveTextContent("Delete Monstera Mike?");

    fireEvent.click(within(dialog).getByRole("button", { name: "Cancel" }));
    await waitFor(() => expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument());
    expect(remove).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole("button", { name: "Delete" }));
    const confirmed = await screen.findByRole("alertdialog");
    fireEvent.click(within(confirmed).getByRole("button", { name: "Delete" }));
    await waitFor(() => expect(remove).toHaveBeenCalledWith(1));
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

    await screen.findByRole("heading", { level: 1, name: "Monstera Mike" });
    expect(screen.queryByText(/Care tips/)).not.toBeInTheDocument();
  });
});
