import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { Breadcrumbs } from "@/components/Breadcrumbs";

describe("Breadcrumbs", () => {
  it("renders links for ancestors and marks the current page", () => {
    render(
      <MemoryRouter>
        <Breadcrumbs
          items={[{ label: "Home", to: "/" }, { label: "Plants", to: "/plants" }, { label: "Monstera Mike" }]}
        />
      </MemoryRouter>,
    );

    const nav = screen.getByRole("navigation", { name: "Breadcrumb" });
    expect(nav).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Plants" })).toBeInTheDocument();
    const current = screen.getByText("Monstera Mike");
    expect(current).toHaveAttribute("aria-current", "page");
    expect(current.tagName).toBe("SPAN");
  });
});
