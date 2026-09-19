import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { CareTipsCard } from "@/components/CareTipsCard";
import type { PlantCareTips } from "@/api/types";

const tips: PlantCareTips = {
  commonName: "Monstera",
  lightRequirement: "Bright",
  humidityNotes: "Loves misting.",
  careTips: [
    "## Watering",
    "",
    "- Let the top layer dry between waterings",
    "- Use [filtered water](https://example.com/water)",
    "",
    "> Rotate for even growth.",
  ].join("\n"),
};

describe("CareTipsCard", () => {
  it("renders care notes through typography prose styling", () => {
    const { container } = render(<CareTipsCard tips={tips} />);

    const notes = container.querySelector(".prose");
    expect(notes).not.toBeNull();
    expect(notes).toHaveClass("prose-sm", "prose-neutral", "dark:prose-invert");
  });

  it("renders structured markdown (headings, lists, links, quotes)", () => {
    render(<CareTipsCard tips={tips} />);

    expect(screen.getByRole("heading", { level: 2, name: "Watering" })).toBeInTheDocument();
    const items = screen.getAllByRole("listitem");
    expect(items).toHaveLength(2);
    const link = screen.getByRole("link", { name: "filtered water" });
    expect(link).toHaveAttribute("href", "https://example.com/water");
    expect(screen.getByRole("blockquote")).toBeInTheDocument();
  });

  it("keeps light and humidity fields rendered as labels", () => {
    render(<CareTipsCard tips={tips} />);

    expect(screen.getByText("Bright, indirect light")).toBeInTheDocument();
    expect(screen.getByText("Loves misting.")).toBeInTheDocument();
  });
});
