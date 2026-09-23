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
    notes: vi.fn(),
    addNote: vi.fn(),
    careTasks: vi.fn(),
    snooze: vi.fn(),
    clearSnooze: vi.fn(),
    soilWet: vi.fn(),
    clearSoilWet: vi.fn(),
    addCareTask: vi.fn(),
    deleteCareTask: vi.fn(),
    markCareTaskDone: vi.fn(),
    healthChecks: vi.fn(),
    addHealthCheck: vi.fn(),
  },
  ApiError: class ApiError extends Error {},
}));

const plant: Plant = {
  id: 1,
  nickName: "Monstera Mike",
  roomId: null,

  roomName: "Living room",
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
  lastWateredAt: "2026-03-01T00:00:00",
  dueStatus: "Overdue",
  wateringIntervalDays: 7,
  daysUntilDue: -3,
  nextDueDate: "2026-03-08T00:00:00",
  dueMessage: "3 days overdue",
  roomLightMatch: null,
  healthStatus: null,
  lastCheckupAt: null,
  checkupDue: false,
};

const logs: WateringLogEntry[] = [
  { id: 2, wateredAt: "2026-03-01T09:00:00", note: "Soaked thoroughly", amountMilliliters: null, method: null },
  { id: 1, wateredAt: "2026-02-20T09:00:00", note: null, amountMilliliters: 250, method: "Filtered" },
];

describe("PlantDetailPage", () => {
  beforeEach(() => {
    vi.mocked(plantsApi.get).mockReset().mockResolvedValue(plant);
    vi.mocked(plantsApi.wateringLogs).mockReset().mockResolvedValue(logs);
    vi.mocked(plantsApi.notes).mockReset().mockResolvedValue([
      { id: 9, createdAt: "2026-03-02T10:00:00Z", text: "New leaf unfurling" },
    ]);
    vi.mocked(plantsApi.addNote).mockReset();
    vi.mocked(plantsApi.healthChecks).mockReset().mockResolvedValue([]);
    vi.mocked(plantsApi.addHealthCheck).mockReset();
    vi.mocked(plantsApi.careTasks).mockReset().mockResolvedValue([
      {
        id: 1,
        type: "Watering",
        intervalDays: 7,
        lastDoneAt: null,
        reduceInWinter: null,
        dueStatus: "Upcoming",
        daysUntilDue: 7,
        nextDueDate: "2026-03-11T00:00:00",
        dueMessage: "7 days until due",
        inWinterNow: false,
        hint: null,
      },
    ]);
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
    expect(screen.getByText("250 ml")).toBeInTheDocument();
    expect(screen.getByText("Filtered")).toBeInTheDocument();
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
      potSizeCm: null,
      soilMix: null,
      propagatedFrom: null,
      notifyEnabled: true,
      snoozedUntil: null,
      soilWetUntil: null,
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
    const fileInput = container.querySelector<HTMLInputElement>('input[type="file"].hidden');
    expect(fileInput).not.toBeNull();

    fireEvent.change(fileInput!, {
      target: { files: [new File(["data"], "new.png", { type: "image/png" })] },
    });

    await waitFor(() => expect(plantsApi.uploadPhoto).toHaveBeenCalledWith(1, expect.any(File)));
    await waitFor(() =>
      expect(container.querySelector('img[src="/uploads/plants/1/new.png"]')).not.toBeNull(),
    );
  });

  it("does not upload an unsupported photo type (e.g. HEIC) client-side", async () => {
    vi.mocked(plantsApi.uploadPhoto).mockClear();
    const { container } = renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });
    await screen.findByRole("heading", { name: "Monstera Mike" });

    const fileInput = container.querySelector<HTMLInputElement>('input[type="file"].hidden');
    fireEvent.change(fileInput!, {
      target: { files: [new File(["data"], "photo.heic", { type: "image/heic" })] },
    });

    expect(plantsApi.uploadPhoto).not.toHaveBeenCalled();
  });

  it("gives the phone action buttons touch-friendly targets", async () => {
    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    const button = await screen.findByRole("button", { name: "Mark as watered" });
    expect(button).toHaveClass("min-h-11", "sm:min-h-9");
  });

  it("places the action buttons above the details section", async () => {
    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    const water = await screen.findByRole("button", { name: "Mark as watered" });
    const details = screen.getByText("Details");
    // The details heading must follow the action row in document order.
    expect(water.compareDocumentPosition(details) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it("renders the photo on the right of the details, matching the card height on lg", async () => {
    vi.mocked(plantsApi.get).mockResolvedValue({
      ...plant,
      photoUrl: "https://example.com/mike.jpg",
    });

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    const img = await screen.findByRole("img", { name: "Monstera Mike" });
    // The img fills an absolutely-positioned wrapper; the wrapper (not the img)
    // carries the stretch classes so flexbox sizes it to the details card.
    const wrapper = img.parentElement as HTMLElement;
    expect(img).toHaveClass("absolute", "inset-0", "h-full");
    expect(wrapper).toHaveClass("aspect-[3/4]", "lg:aspect-auto", "lg:self-stretch", "lg:order-2");
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

  it("sends optional amount and method with the watering", async () => {
    vi.mocked(plantsApi.water).mockResolvedValue(plant);
    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    fireEvent.change(await screen.findByLabelText("ml (optional)"), { target: { value: "250" } });
    fireEvent.click(screen.getByRole("combobox", { name: /Method/i }));
    fireEvent.click(await screen.findByRole("option", { name: "Filtered" }));
    fireEvent.click(screen.getByRole("button", { name: "Mark as watered" }));

    await waitFor(() =>
      expect(plantsApi.water).toHaveBeenCalledWith(1, { amountMilliliters: 250, method: "Filtered" }),
    );
  });

  it("lists plant notes and adds new ones", async () => {
    vi.mocked(plantsApi.addNote).mockResolvedValue({
      id: 10,
      createdAt: new Date().toISOString(),
      text: "Yellowing tip",
    });

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    expect(await screen.findByText("New leaf unfurling")).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("New note"), { target: { value: "Yellowing tip" } });
    fireEvent.click(screen.getByRole("button", { name: "Add note" }));

    await waitFor(() => expect(plantsApi.addNote).toHaveBeenCalledWith(1, "Yellowing tip"));
  });

  it("logs a one-tap health checkup with an optional note", async () => {
    vi.mocked(plantsApi.get).mockResolvedValue({ ...plant, checkupDue: true });
    vi.mocked(plantsApi.addHealthCheck).mockResolvedValue({
      ...plant,
      healthStatus: "Sick",
      lastCheckupAt: "2026-03-04T10:00:00",
      checkupDue: false,
    });

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    expect(await screen.findByText("Monthly checkup due — how is this plant doing?")).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Checkup note (optional)"), { target: { value: "Mushy stem" } });
    fireEvent.click(screen.getByRole("button", { name: "Sick" }));

    await waitFor(() =>
      expect(plantsApi.addHealthCheck).toHaveBeenCalledWith(1, { status: "Sick", note: "Mushy stem" }),
    );
  });

  it("shows the current health status and past checkups", async () => {
    vi.mocked(plantsApi.get).mockResolvedValue({
      ...plant,
      healthStatus: "Excellent",
      lastCheckupAt: "2026-03-01T10:00:00",
    });
    vi.mocked(plantsApi.healthChecks).mockResolvedValue([
      { id: 2, status: "Excellent", checkedAt: "2026-03-01T10:00:00", note: null },
      { id: 1, status: "Bad", checkedAt: "2026-02-01T10:00:00", note: "Dropping leaves" },
    ]);

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    expect(await screen.findByText(/Last checkup/)).toBeInTheDocument();
    expect(screen.getAllByText("Excellent").length).toBeGreaterThanOrEqual(1);
    expect(await screen.findByText("Checkup history")).toBeInTheDocument();
    expect(screen.getByText("Dropping leaves")).toBeInTheDocument();
  });

  it("snoozes reminders for the selected length", async () => {
    vi.mocked(plantsApi.snooze).mockResolvedValue({
      ...plant,
      snoozedUntil: new Date(Date.now() + 14 * 86_400_000).toISOString(),
    });

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    await screen.findByRole("heading", { level: 1, name: "Monstera Mike" });
    fireEvent.change(screen.getByLabelText("Snooze days"), { target: { value: "30" } });
    fireEvent.click(screen.getByRole("button", { name: "Snooze reminders" }));

    await waitFor(() => expect(plantsApi.snooze).toHaveBeenCalledWith(1, 30));
  });

  it("defers watering when the soil is still wet", async () => {
    vi.mocked(plantsApi.soilWet).mockResolvedValue({
      ...plant,
      soilWetUntil: new Date(Date.now() + 5 * 86_400_000).toISOString(),
    });

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    await screen.findByRole("heading", { level: 1, name: "Monstera Mike" });
    fireEvent.change(screen.getByLabelText("Recheck watering in days"), { target: { value: "5" } });
    fireEvent.click(screen.getByRole("button", { name: "Soil is still wet" }));

    await waitFor(() => expect(plantsApi.soilWet).toHaveBeenCalledWith(1, 5));
  });

  it("shows an active deferral and resumes on check-now", async () => {
    vi.mocked(plantsApi.get).mockResolvedValue({
      ...plant,
      soilWetUntil: new Date(Date.now() + 4 * 86_400_000).toISOString(),
      dueStatus: "Upcoming",
      daysUntilDue: 4,
    });
    vi.mocked(plantsApi.clearSoilWet).mockResolvedValue({ ...plant, soilWetUntil: null });

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    expect(await screen.findByText(/deferred until/)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Soil is still wet" })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Check now" }));
    await waitFor(() => expect(plantsApi.clearSoilWet).toHaveBeenCalledWith(1));
  });

  it("lists care tasks with due info and marks them done", async () => {
    vi.mocked(plantsApi.markCareTaskDone).mockResolvedValue({
      id: 1,
      type: "Watering",
      intervalDays: 7,
      lastDoneAt: new Date().toISOString(),
      reduceInWinter: null,
      dueStatus: "Upcoming",
      daysUntilDue: 7,
      nextDueDate: "2026-03-11T00:00:00",
      dueMessage: "7 days until due",
      inWinterNow: false,
      hint: null,
    });

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    expect(await screen.findByText("Watering")).toBeInTheDocument();
    expect(screen.getByText("7 days until due")).toBeInTheDocument();

    fireEvent.click(screen.getAllByRole("button", { name: "Mark done" })[0]);
    await waitFor(() => expect(plantsApi.markCareTaskDone).toHaveBeenCalledWith(1, "Watering"));
  });

  it("adds a fertilizing schedule", async () => {
    vi.mocked(plantsApi.addCareTask).mockResolvedValue({
      id: 2,
      type: "Fertilizing",
      intervalDays: 30,
      lastDoneAt: null,
      reduceInWinter: true,
      dueStatus: "NotScheduled",
      daysUntilDue: null,
      nextDueDate: null,
      dueMessage: "No watering schedule",
      inWinterNow: false,
      hint: null,
    });

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    await screen.findByText("Watering");
    fireEvent.change(screen.getByLabelText("Every (days)"), { target: { value: "30" } });
    fireEvent.click(screen.getByRole("button", { name: "Add fertilizing" }));

    await waitFor(() =>
      expect(plantsApi.addCareTask).toHaveBeenCalledWith(1, {
        type: "Fertilizing",
        intervalDays: 30,
        reduceInWinter: true,
      }),
    );
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
      profileToxicToPets: false,
      profileToxicToChildren: false,
      careTips: {
        commonName: "Monstera",
        lightRequirement: "Bright",
        humidityNotes: "Loves misting.",
        careTips: "Feed **monthly** in summer.",
        diagnosisChecklist: JSON.stringify([
          { symptom: "Yellow leaves", causes: ["Overwatering", "Normal leaf loss"] },
        ]),
      },
    });

    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    expect(await screen.findByText(/Care tips — Monstera/)).toBeInTheDocument();
    expect(screen.getByText("Bright, indirect light")).toBeInTheDocument();
    expect(screen.getByText("Loves misting.")).toBeInTheDocument();
    expect(screen.getByText("monthly").closest("strong")).not.toBeNull();
    expect(screen.getByText("When something's off — Monstera")).toBeInTheDocument();
    const symptom = screen.getByText("Yellow leaves");
    expect(symptom.closest("details")).not.toBeNull();
    expect(screen.getByText("Normal leaf loss")).toBeInTheDocument();
  });

  it("hides the care tips section for plants without a profile", async () => {
    renderWithProviders(<PlantDetailPage />, { path: "/plants/:id", route: "/plants/1" });

    await screen.findByRole("heading", { level: 1, name: "Monstera Mike" });
    expect(screen.queryByText(/Care tips/)).not.toBeInTheDocument();
  });
});
