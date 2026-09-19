import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { PlantPhoto } from "@/components/PlantPhoto";

describe("PlantPhoto", () => {
  it("renders the plant photo when a URL is set", () => {
    render(<PlantPhoto photoUrl="https://example.com/pothos.jpg" nickName="Pothos" />);

    const img = screen.getByRole("img", { name: "Pothos" });
    expect(img).toHaveAttribute("src", "https://example.com/pothos.jpg");
    expect(img).toHaveAttribute("loading", "lazy");
  });

  it("shows the placeholder illustration when there is no photo", () => {
    render(<PlantPhoto photoUrl={null} nickName="Pothos" />);

    expect(screen.getByRole("img", { name: "No photo of Pothos" })).toBeInTheDocument();
  });

  it("falls back to the placeholder when the image fails to load", () => {
    render(<PlantPhoto photoUrl="https://example.com/broken.jpg" nickName="Pothos" />);

    fireEvent.error(screen.getByRole("img", { name: "Pothos" }));

    expect(screen.queryByRole("img", { name: "Pothos" })).not.toBeInTheDocument();
    expect(screen.getByRole("img", { name: "No photo of Pothos" })).toBeInTheDocument();
  });
});
