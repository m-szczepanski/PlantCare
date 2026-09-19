import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { roomsApi } from "@/api/client";
import type { Room } from "@/api/types";
import RoomsPage from "@/pages/RoomsPage";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  roomsApi: { list: vi.fn(), create: vi.fn(), update: vi.fn(), remove: vi.fn() },
  ApiError: class ApiError extends Error {},
}));

const rooms: Room[] = [
  { id: 1, name: "Living room", orientation: "South", plantCount: 2 },
  { id: 2, name: "Hallway", orientation: null, plantCount: 0 },
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

  it("creates a room through the API", async () => {
    vi.mocked(roomsApi.create).mockResolvedValue({ id: 3, name: "Study", orientation: null, plantCount: 0 });

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
