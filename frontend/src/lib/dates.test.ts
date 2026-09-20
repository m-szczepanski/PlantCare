import { describe, expect, it } from "vitest";
import { formatInstant } from "@/lib/dates";

describe("formatInstant", () => {
  it("reads naive UTC instants as UTC, not local time", () => {
    expect(formatInstant("2026-03-01T09:00:00")).toBe(new Date("2026-03-01T09:00:00Z").toLocaleString());
    expect(formatInstant("2026-03-01T09:00:00Z")).toBe(new Date("2026-03-01T09:00:00Z").toLocaleString());
  });

  it("renders date-only mode", () => {
    expect(formatInstant("2026-03-01T09:00:00", false)).toBe(
      new Date("2026-03-01T09:00:00Z").toLocaleDateString(),
    );
  });

  it("passes through values that already carry a timezone designator", () => {
    expect(formatInstant("2026-09-20T13:36:35+02:00")).toBe(
      new Date("2026-09-20T13:36:35+02:00").toLocaleString(),
    );
  });

  it("handles null", () => {
    expect(formatInstant(null)).toBe("-");
  });
});
