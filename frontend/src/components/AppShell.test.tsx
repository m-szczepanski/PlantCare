import { describe, expect, it } from "vitest";
import { screen } from "@testing-library/react";
import { AppShell } from "@/components/AppShell";
import { ThemeProvider } from "@/components/ThemeProvider";
import { renderWithProviders } from "@/test/render";

function renderShell(route: string) {
  return renderWithProviders(
    <ThemeProvider>
      <AppShell>
        <div>page content</div>
      </AppShell>
    </ThemeProvider>,
    { route },
  );
}

describe("AppShell", () => {
  it("renders persistent nav, theme toggle, and page content", () => {
    renderShell("/");

    expect(screen.getByText("page content")).toBeInTheDocument();
    const trigger = screen.getByRole("button", { name: "Toggle Sidebar" });
    expect(trigger).toBeInTheDocument();
    expect(trigger).toHaveClass("h-11", "sm:h-9");
    expect(screen.getByRole("button", { name: "Toggle theme" })).toHaveClass("h-11", "sm:h-9");
    expect(screen.getByRole("link", { name: "Dashboard" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Plants" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Add plant" })).toBeInTheDocument();
  });

  it("marks the active nav item on the dashboard", () => {
    renderShell("/");

    const dashboard = screen.getByRole("link", { name: "Dashboard" });
    expect(dashboard).toHaveAttribute("aria-current", "page");
    expect(dashboard.closest("[data-sidebar='menu-button']")).toHaveAttribute(
      "data-active",
      "true",
    );
    expect(screen.getByRole("link", { name: "Plants" })).not.toHaveAttribute("aria-current");
  });

  it("marks Plants active for nested plant routes", () => {
    renderShell("/plants");

    const plants = screen.getByRole("link", { name: "Plants" });
    expect(plants).toHaveAttribute("aria-current", "page");
    expect(screen.getByRole("link", { name: "Add plant" })).not.toHaveAttribute(
      "aria-current",
    );
  });
});
