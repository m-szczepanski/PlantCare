import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import i18n from "@/i18n";
import { plantProfilesApi, plantsApi, roomsApi } from "@/api/client";
import type { Plant, Room } from "@/api/types";
import { SetupWizard } from "@/components/SetupWizard";
import { ThemeProvider } from "@/components/ThemeProvider";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  plantsApi: { list: vi.fn(), create: vi.fn() },
  roomsApi: { list: vi.fn(), create: vi.fn(), remove: vi.fn() },
  plantProfilesApi: { list: vi.fn() },
  ApiError: class ApiError extends Error {},
}));

vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

let rooms: Room[] = [];
let plants: Plant[] = [];

function renderWizard(onFinished = vi.fn()) {
  const result = renderWithProviders(
    <ThemeProvider>
      <SetupWizard onFinished={onFinished} />
    </ThemeProvider>,
  );
  return { ...result, onFinished };
}

beforeEach(() => {
  rooms = [];
  plants = [];
  void i18n.changeLanguage("en");
  vi.mocked(plantProfilesApi.list).mockReset().mockResolvedValue([]);
  vi.mocked(roomsApi.list).mockReset().mockImplementation(async () => [...rooms]);
  vi.mocked(roomsApi.create).mockReset().mockImplementation(async (input) => {
    const room: Room = {
      id: rooms.length + 1,
      name: input.name,
      orientation: null,
      lightExposure: null,
      humidity: null,
      temperatureCelsius: null,
      plantCount: 0,
    };
    rooms.push(room);
    return room;
  });
  vi.mocked(roomsApi.remove).mockReset().mockImplementation(async (id: number) => {
    rooms = rooms.filter((room) => room.id !== id);
  });
  vi.mocked(plantsApi.list).mockReset().mockImplementation(async () => [...plants]);
  vi.mocked(plantsApi.create).mockReset().mockImplementation(async (input) => {
    const plant = { id: plants.length + 1, nickName: input.nickName } as Plant;
    plants.push(plant);
    return plant;
  });
});

describe(
  "SetupWizard",
  { timeout: 30_000 },
  () => {
    it("walks through rooms, plants, and preferences, then finishes", async () => {
      const { onFinished } = renderWizard();

      expect(screen.getByText("Welcome to PlantCare")).toBeInTheDocument();
      expect(screen.getByText("Step 1 of 5")).toBeInTheDocument();
      fireEvent.click(screen.getByRole("button", { name: "Let's go" }));

      expect(screen.getByText("Rooms")).toBeInTheDocument();
      expect(screen.getByText("Step 2 of 5")).toBeInTheDocument();
      fireEvent.change(screen.getByLabelText("Add room"), { target: { value: "Kitchen" } });
      fireEvent.click(screen.getByRole("button", { name: "Add" }));
      expect(await screen.findByText("Kitchen")).toBeInTheDocument();
      fireEvent.click(screen.getByRole("button", { name: "Next" }));

      expect(screen.getByText("Plants")).toBeInTheDocument();
      fireEvent.change(screen.getByLabelText("Nick name"), { target: { value: "Monstera Mike" } });
      fireEvent.click(screen.getByRole("button", { name: "Add plant" }));
      await waitFor(() => expect(plantsApi.create).toHaveBeenCalled());
      expect(await screen.findByText("1 plant added so far")).toBeInTheDocument();
      fireEvent.click(screen.getByRole("button", { name: "Next" }));

      expect(screen.getByText("Theme & language")).toBeInTheDocument();
      fireEvent.click(screen.getByRole("button", { name: "Dark" }));
      expect(window.localStorage.getItem("ui-theme")).toBe("dark");
      expect(screen.getByRole("button", { name: "Dark" })).toHaveAttribute("aria-pressed", "true");
      fireEvent.click(screen.getByRole("button", { name: "Polski" }));
      expect(await screen.findByText("Motyw i język")).toBeInTheDocument();
      void i18n.changeLanguage("en");
      fireEvent.click(await screen.findByRole("button", { name: "Next" }));

      expect(screen.getByText("Rooms added: 1")).toBeInTheDocument();
      expect(screen.getByText("Plants added: 1")).toBeInTheDocument();
      fireEvent.click(screen.getByRole("button", { name: "Go to dashboard" }));
      expect(onFinished).toHaveBeenCalled();
    });

    it("lets the user skip rooms and go straight to plants", async () => {
      renderWizard();
      fireEvent.click(screen.getByRole("button", { name: "Let's go" }));
      fireEvent.click(screen.getByRole("button", { name: "Skip for now" }));

      expect(screen.getByText("Plants")).toBeInTheDocument();
      fireEvent.change(screen.getByLabelText("Nick name"), { target: { value: "Pothos Pat" } });
      fireEvent.click(screen.getByRole("button", { name: "Add plant" }));
      await waitFor(() => expect(plantsApi.create).toHaveBeenCalledWith(
        expect.objectContaining({ nickName: "Pothos Pat", roomId: null }),
      ));
    });

    it("can remove a room added during setup", async () => {
      renderWizard();
      fireEvent.click(screen.getByRole("button", { name: "Let's go" }));
      fireEvent.change(screen.getByLabelText("Add room"), { target: { value: "Kitchen" } });
      fireEvent.click(screen.getByRole("button", { name: "Add" }));
      const kitchen = await screen.findByText("Kitchen");
      expect(kitchen).toBeInTheDocument();

      fireEvent.click(screen.getByRole("button", { name: "Remove Kitchen" }));
      await waitFor(() => expect(roomsApi.remove).toHaveBeenCalledWith(1));
      await waitFor(() => expect(screen.queryByText("Kitchen")).not.toBeInTheDocument());
    });
  },
);
