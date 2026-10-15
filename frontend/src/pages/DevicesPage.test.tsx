import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { devicesApi, healthApi } from "@/api/client";
import type { Device } from "@/api/types";
import DevicesPage from "@/pages/DevicesPage";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  devicesApi: { list: vi.fn(), create: vi.fn(), update: vi.fn(), remove: vi.fn() },
  plantsApi: { deviceWater: vi.fn() },
  healthApi: { status: vi.fn().mockResolvedValue({ devicesEnabled: true }) },
  ApiError: class ApiError extends Error {},
}));

const devices: Device[] = [
  {
    id: 1,
    name: "Pump 1",
    type: "Pump",
    commandStyle: "Pulse",
    commandTopic: null,
    stateTopic: null,
    lastMoisturePct: 42,
    waterLevel: "Ok",
    lastSeenAt: "2026-10-15T08:00:00Z",
    plantCount: 1,
  },
];

beforeEach(() => {
  vi.mocked(devicesApi.list).mockReset().mockResolvedValue(devices);
  vi.mocked(devicesApi.create).mockReset();
  vi.mocked(devicesApi.update).mockReset();
  vi.mocked(devicesApi.remove).mockReset();
  vi.mocked(healthApi.status).mockReset().mockResolvedValue({ devicesEnabled: true } as never);
});

describe("DevicesPage", () => {
  it("lists devices with type, command style and moisture", async () => {
    renderWithProviders(<DevicesPage />, { route: "/devices" });

    expect(await screen.findByText("Pump 1")).toBeInTheDocument();
    expect(screen.getByText(/Pulse/)).toBeInTheDocument();
    expect(screen.getByText("42%")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Add device" })).toBeInTheDocument();
  });

  it("shows a notice when devices are disabled", async () => {
    vi.mocked(healthApi.status).mockResolvedValue({ devicesEnabled: false } as never);

    renderWithProviders(<DevicesPage />, { route: "/devices" });

    expect(await screen.findByText("Devices are not enabled")).toBeInTheDocument();
    expect(devicesApi.list).not.toHaveBeenCalled();
  });
});
