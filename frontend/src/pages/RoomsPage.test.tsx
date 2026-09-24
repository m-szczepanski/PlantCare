import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { roomsApi } from "@/api/client";
import type { Room } from "@/api/types";
import RoomsPage from "@/pages/RoomsPage";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  roomsApi: { list: vi.fn(), create: vi.fn(), update: vi.fn(), remove: vi.fn() },
  plantsApi: { list: vi.fn().mockResolvedValue([]) },
  ApiError: class ApiError extends Error {},
}));

const rooms: Room[] = [
  { id: 1, name: "Living room", orientation: "South", lightExposure: "Bright", humidity: "Medium", temperatureCelsius: 21, plantCount: 2 },
  { id: 2, name: "Hallway", orientation: null, lightExposure: null, humidity: null, temperatureCelsius: null, plantCount: 0 },
];

beforeEach(() => {
  vi.mocked(roomsApi.list).mockReset().mockResolvedValue(rooms);
  vi.mocked(roomsApi.create).mockReset();
  vi.mocked(roomsApi.update).mockReset();
  vi.mocked(roomsApi.remove).mockReset();
});

describe("RoomsPage", () => {
  it("lists rooms with orientation and plant counts", async () => {
    renderWithProviders(<RoomsPage />, { route: "/rooms" });

    expect(await screen.findByText("Living room")).toBeInTheDocument();
    expect(screen.getByText(/South-facing · 2 plants/)).toBeInTheDocument();
    expect(screen.getByText(/No orientation set · 0 plants/)).toBeInTheDocument();
  });

  it("displays properties read-only and saves edits only after entering edit mode", async () => {
    vi.mocked(roomsApi.update).mockResolvedValue(rooms[0]);

    renderWithProviders(<RoomsPage />, { route: "/rooms" });

    expect(await screen.findByText("Medium humidity")).toBeInTheDocument();
    expect(screen.getByText("21°C")).toBeInTheDocument();
    expect(screen.queryByLabelText("Average temperature (°C)")).not.toBeInTheDocument();

    const livingRoom = screen.getByText("Living room");
    const card = livingRoom.closest<HTMLElement>("div.rounded-xl")!;
    fireEvent.click(within(card).getByRole("button", { name: "Edit" }));

    fireEvent.change(within(card).getByLabelText("Average temperature (°C)"), { target: { value: "19" } });
    const save = within(card).getByRole("button", { name: "Save" });
    expect(save).toBeEnabled();
    fireEvent.click(save);

    await waitFor(() =>
      expect(roomsApi.update).toHaveBeenCalledWith(1, {
        name: "Living room",
        orientation: "South",
        lightExposure: "Bright",
        humidity: "Medium",
        temperatureCelsius: 19,
      }),
    );
  });

  it("discards changes and returns to read-only view on cancel", async () => {
    vi.mocked(roomsApi.update).mockResolvedValue(rooms[0]);

    renderWithProviders(<RoomsPage />, { route: "/rooms" });

    const livingRoom = await screen.findByText("Living room");
    const card = livingRoom.closest<HTMLElement>("div.rounded-xl")!;
    fireEvent.click(within(card).getByRole("button", { name: "Edit" }));

    const temperature = within(card).getByLabelText("Average temperature (°C)");
    fireEvent.change(temperature, { target: { value: "19" } });
    fireEvent.click(within(card).getByRole("button", { name: "Cancel" }));

    expect(roomsApi.update).not.toHaveBeenCalled();
    expect(within(card).queryByLabelText("Average temperature (°C)")).not.toBeInTheDocument();
    expect(within(card).getByText("21°C")).toBeInTheDocument();
  });

  it("creates a room through the API", async () => {
    vi.mocked(roomsApi.create).mockResolvedValue({ id: 3, name: "Study", orientation: null, lightExposure: null, humidity: null, temperatureCelsius: null, plantCount: 0 });

    renderWithProviders(<RoomsPage />, { route: "/rooms" });

    await screen.findByText("Living room");
    fireEvent.change(screen.getByLabelText("New room name"), { target: { value: "Study" } });
    fireEvent.click(screen.getByRole("button", { name: "Add room" }));

    await waitFor(() => expect(roomsApi.create).toHaveBeenCalledWith({ name: "Study" }));
  });

  it("deletes a room behind a confirm dialog", async () => {
    vi.mocked(roomsApi.remove).mockResolvedValue(undefined);

    renderWithProviders(<RoomsPage />, { route: "/rooms" });

    const hallway = await screen.findByText("Hallway");
    const card = hallway.closest<HTMLElement>("div.rounded-xl")!;
    fireEvent.click(within(card).getByRole("button", { name: "Delete" }));

    const dialog = await screen.findByRole("alertdialog");
    expect(dialog).toHaveTextContent("Delete Hallway?");
    fireEvent.click(within(dialog).getByRole("button", { name: "Delete" }));

    await waitFor(() => expect(roomsApi.remove).toHaveBeenCalledWith(2));
  });
});
