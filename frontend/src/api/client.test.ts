import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError, dashboardApi, plantsApi } from "@/api/client";

function jsonResponse(status: number, body: unknown) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

describe("plantsApi client", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("list() hits GET /api/plants", async () => {
    vi.mocked(fetch).mockResolvedValue(jsonResponse(200, [{ id: 1 }]));

    const result = await plantsApi.list();

    expect(fetch).toHaveBeenCalledWith("/api/plants", expect.objectContaining({}));
    expect(result).toEqual([{ id: 1 }]);
  });

  it("create() POSTs JSON payload", async () => {
    vi.mocked(fetch).mockResolvedValue(jsonResponse(201, { id: 5, nickName: "Fig" }));

    await plantsApi.create({ nickName: "Fig", location: "Kitchen", acquiredDate: "2026-01-01T00:00:00" });

    const [url, init] = vi.mocked(fetch).mock.calls[0];
    expect(url).toBe("/api/plants");
    expect(init?.method).toBe("POST");
    expect(JSON.parse(init?.body as string).nickName).toBe("Fig");
  });

  it("remove() sends DELETE", async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(null, { status: 204 }));

    await expect(plantsApi.remove(3)).resolves.toBeUndefined();
    const [, init] = vi.mocked(fetch).mock.calls[0];
    expect(init?.method).toBe("DELETE");
  });

  it("throws ApiError with validation detail on 400", async () => {
    vi.mocked(fetch).mockResolvedValue(
      jsonResponse(400, { title: "One or more validation errors occurred.", errors: { NickName: ["The NickName field is required."] } }),
    );

    await expect(plantsApi.create({ nickName: "", location: "x", acquiredDate: "2026-01-01T00:00:00" })).rejects.toMatchObject({
      status: 400,
      detail: "The NickName field is required.",
    });
  });

  it("exposes ApiError instances", async () => {
    vi.mocked(fetch).mockResolvedValue(jsonResponse(404, { detail: "nope" }));

    const error = await plantsApi.get(99).catch((e) => e);
    expect(error).toBeInstanceOf(ApiError);
  });

  it("dashboardApi.get() hits GET /api/dashboard", async () => {
    vi.mocked(fetch).mockResolvedValue(jsonResponse(200, { overdue: [], dueToday: [], upcoming: [] }));

    const result = await dashboardApi.get();

    expect(fetch).toHaveBeenCalledWith("/api/dashboard", expect.objectContaining({}));
    expect(result).toEqual({ overdue: [], dueToday: [], upcoming: [] });
  });
});
