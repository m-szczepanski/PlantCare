import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { plantsApi, roomsApi } from "@/api/client";
import type { Plant, Room } from "@/api/types";
import { SetupGate } from "@/components/SetupGate";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  plantsApi: { list: vi.fn(), create: vi.fn() },
  roomsApi: { list: vi.fn(), create: vi.fn(), remove: vi.fn() },
  plantProfilesApi: { list: vi.fn().mockResolvedValue([]) },
  ApiError: class ApiError extends Error {},
}));

vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

const room: Room = {
  id: 1,
  name: "Living Room",
  orientation: null,
  lightExposure: null,
  humidity: null,
  temperatureCelsius: null,
  plantCount: 0,
};

const plant = { id: 1, nickName: "Monstera" } as Plant;

function renderGate() {
  return renderWithProviders(
    <SetupGate>
      <div>app content</div>
    </SetupGate>,
  );
}

beforeEach(() => {
  vi.mocked(plantsApi.list).mockReset();
  vi.mocked(roomsApi.list).mockReset();
});

describe("SetupGate", () => {
  it("launches the setup wizard when there are no rooms and no plants", async () => {
    vi.mocked(plantsApi.list).mockResolvedValue([]);
    vi.mocked(roomsApi.list).mockResolvedValue([]);

    renderGate();

    expect(await screen.findByText("Welcome to PlantCare")).toBeInTheDocument();
    expect(screen.queryByText("app content")).not.toBeInTheDocument();
  });

  it("renders the app when at least one plant exists", async () => {
    vi.mocked(plantsApi.list).mockResolvedValue([plant]);
    vi.mocked(roomsApi.list).mockResolvedValue([]);

    renderGate();

    expect(await screen.findByText("app content")).toBeInTheDocument();
    expect(screen.queryByText("Welcome to PlantCare")).not.toBeInTheDocument();
  });

  it("renders the app when at least one room exists", async () => {
    vi.mocked(plantsApi.list).mockResolvedValue([]);
    vi.mocked(roomsApi.list).mockResolvedValue([room]);

    renderGate();

    expect(await screen.findByText("app content")).toBeInTheDocument();
  });

  it("renders the app when the first-run check fails", async () => {
    vi.mocked(plantsApi.list).mockRejectedValue(new Error("offline"));
    vi.mocked(roomsApi.list).mockResolvedValue([]);

    renderGate();

    expect(await screen.findByText("app content")).toBeInTheDocument();
  });
});
