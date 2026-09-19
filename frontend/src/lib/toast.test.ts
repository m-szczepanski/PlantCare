import { describe, expect, it, vi } from "vitest";
import { toast } from "sonner";
import { ApiError } from "@/api/client";
import { errorMessage, toastError } from "@/lib/toast";

vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

describe("toast helpers", () => {
  it("prefers the API validation detail when present", () => {
    const error = new ApiError(400, "Request to /plants failed (400)", "NickName is required");
    expect(errorMessage(error)).toBe("NickName is required");
  });

  it("falls back to the error message", () => {
    expect(errorMessage(new ApiError(500, "Request failed"))).toBe("Request failed");
    expect(errorMessage(new Error("boom"))).toBe("boom");
    expect(errorMessage("nope")).toBe("Unknown error");
  });

  it("toastError forwards title and description to sonner", () => {
    toastError("Could not add plant", new Error("boom"));
    expect(toast.error).toHaveBeenCalledWith("Could not add plant", { description: "boom" });
  });
});
