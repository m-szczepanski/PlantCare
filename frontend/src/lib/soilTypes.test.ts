import { describe, expect, it } from "vitest";
import type { SoilTypeOption } from "@/api/types";
import { applySoilFactor, soilFactor } from "@/lib/soilTypes";

const options: SoilTypeOption[] = [
  { type: "AllPurpose", wateringIntervalFactor: 1, mixes: ["All-purpose potting mix", "Worm casting boost", "Leaf mold & loam"] },
  { type: "CactusMix", wateringIntervalFactor: 0.7, mixes: ["Cactus & succulent mix", "Pumice-heavy inorganic mix"] },
  { type: "ChunkyBark", wateringIntervalFactor: 0.55, mixes: ["Aroid chunky blend", "Orchid bark mix"] },
  { type: "PeatCoco", wateringIntervalFactor: 1.15, mixes: ["Peat & perlite mix", "Coco coir & perlite blend", "Sphagnum moss"] },
  { type: "SemiHydro", wateringIntervalFactor: 1.3, mixes: ["Semi-hydro LECA"] },
  { type: "SelfWatering", wateringIntervalFactor: 1.5, mixes: ["Self-watering pot blend"] },
];

describe("soilFactor", () => {
  it("is 1 for no soil type selected", () => {
    expect(soilFactor(options, null)).toBe(1);
  });

  it("is 1 for all-purpose", () => {
    expect(soilFactor(options, "AllPurpose")).toBe(1);
  });

  it("returns the option's factor when known", () => {
    expect(soilFactor(options, "SemiHydro")).toBe(1.3);
  });

  it("falls back to 1 for an unknown type", () => {
    expect(soilFactor(options, "ChunkyBark" as never)).toBe(0.55);
    expect(soilFactor([], "SemiHydro")).toBe(1);
  });
});

describe("applySoilFactor", () => {
  it("returns null for a null / invalid base interval", () => {
    expect(applySoilFactor(options, "SemiHydro", null)).toBeNull();
    expect(applySoilFactor(options, "SemiHydro", 0)).toBeNull();
    expect(applySoilFactor(options, "SemiHydro", 3.5)).toBeNull();
  });

  it("rounds away from zero like the backend", () => {
    expect(applySoilFactor(options, "ChunkyBark", 10)).toBe(6); // 5.5 -> 6
    expect(applySoilFactor(options, "PeatCoco", 10)).toBe(12); // 11.5 -> 12
  });

  it("scales up and down", () => {
    expect(applySoilFactor(options, "CactusMix", 10)).toBe(7);
    expect(applySoilFactor(options, "SelfWatering", 10)).toBe(15);
  });

  it("never drops below one day", () => {
    expect(applySoilFactor(options, "ChunkyBark", 1)).toBe(1); // 0.55 -> rounds to 1
  });

  it("leaves the interval untouched without a soil type", () => {
    expect(applySoilFactor(options, null, 8)).toBe(8);
  });
});
