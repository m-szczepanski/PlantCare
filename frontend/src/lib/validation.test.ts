import { describe, expect, it } from "vitest";
import { ApiError } from "@/api/client";
import { splitApiError } from "@/lib/validation";

describe("splitApiError", () => {
  it("maps PascalCase validation keys to camelCase fields", () => {
    const error = new ApiError(400, "failed", undefined, {
      NickName: ["The NickName field is required."],
      AcquiredDate: ["The value is invalid."],
    });

    const split = splitApiError(error);

    expect(split.banner).toBeNull();
    expect(split.fields).toEqual({
      nickName: "The NickName field is required.",
      acquiredDate: "The value is invalid.",
    });
  });

  it("falls back to the detail as a banner", () => {
    const split = splitApiError(new ApiError(400, "failed", "Unknown plant profile."));

    expect(split.banner).toBe("Unknown plant profile.");
    expect(split.fields).toEqual({});
  });

  it("falls back to the message for generic errors", () => {
    expect(splitApiError(new Error("boom")).banner).toBe("boom");
    expect(splitApiError(null)).toEqual({ banner: null, fields: {} });
  });
});
