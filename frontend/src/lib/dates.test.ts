import { describe, expect, it } from "vitest";
import { dateKey, formatDate, formatInstant, parseDateOnly } from "@/lib/dates";

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

describe("parseDateOnly", () => {
  it("reads a date-only value as the same calendar day regardless of timezone", () => {
    const parsed = parseDateOnly("2026-10-20T00:00:00");
    expect(parsed.getFullYear()).toBe(2026);
    expect(parsed.getMonth()).toBe(9);
    expect(parsed.getDate()).toBe(20);
    expect(parseDateOnly("2026-10-20").getDate()).toBe(20);
  });
});

describe("formatDate", () => {
  it("formats the calendar day without a timezone shift", () => {
    expect(formatDate("2026-10-20T00:00:00", "en")).toBe(parseDateOnly("2026-10-20").toLocaleDateString("en"));
  });

  it("handles null", () => {
    expect(formatDate(null, "en")).toBe("-");
  });
});

describe("dateKey", () => {
  it("formats the local calendar day as yyyy-mm-dd", () => {
    expect(dateKey(new Date(2026, 9, 20))).toBe("2026-10-20");
    expect(dateKey(new Date(2026, 0, 5))).toBe("2026-01-05");
  });
});
