import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { backupApi, healthApi } from "@/api/client";
import type { StatusInfo } from "@/api/types";
import StatusPage from "@/pages/StatusPage";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  healthApi: { status: vi.fn() },
  backupApi: { exportUrl: "/api/export", importDocument: vi.fn() },
  ApiError: class ApiError extends Error {},
}));

const status: StatusInfo = {
  nowUtc: "2026-09-20T07:00:00Z",
  timeZoneId: "UTC",
  wateringCheckCron: "0 8 * * *",
  lastJobRun: { ranAt: "2026-09-20T06:00:00Z", outcome: "digest-sent", sentDigests: 1, failed: 0 },
  lastDigest: { sentAt: "2026-09-20T06:00:00Z", plantCount: 3, overdueCount: 1, priority: 5 },
  ntfy: {
    baseUrl: "http://ntfy:80",
    publicBaseUrl: "http://192.168.0.2:8080",
    topic: "plant-care",
    subscribeUrl: "http://192.168.0.2:8080/plant-care",
    reachable: true,
    latencyMs: 12,
    error: null,
  },
};

beforeEach(() => {
  vi.mocked(healthApi.status).mockReset().mockResolvedValue(status);
  vi.mocked(backupApi.importDocument).mockReset();
});

describe("StatusPage", () => {
  it("shows scheduler, ntfy and instance details", async () => {
    renderWithProviders(<StatusPage />, { route: "/status" });

    expect(await screen.findByText(/digest sent/i)).toBeInTheDocument();
    expect(screen.getByText(/Reachable \(12 ms\)/)).toBeInTheDocument();
    expect(screen.getByText("http://192.168.0.2:8080")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Open subscription page" })).toHaveAttribute(
      "href",
      "http://192.168.0.2:8080/plant-care",
    );
    expect(screen.getByText(/Server time zone: UTC/)).toBeInTheDocument();
  });

  it("imports a JSON backup file through the API", async () => {
    vi.mocked(backupApi.importDocument).mockResolvedValue({
      roomsCreated: 1,
      profilesCreated: 2,
      plantsCreated: 3,
      plantsSkipped: 0,
    });

    const { container } = renderWithProviders(<StatusPage />, { route: "/status" });
    await screen.findByText(/digest sent/i);

    const file = new File([JSON.stringify({ schemaVersion: 1 })], "backup.json", {
      type: "application/json",
    });
    const input = container.querySelector<HTMLInputElement>('input[type="file"].hidden');
    expect(input).not.toBeNull();
    fireEvent.change(input!, { target: { files: [file] } });

    await waitFor(() => expect(backupApi.importDocument).toHaveBeenCalledTimes(1));
  });
});
