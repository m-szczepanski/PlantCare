import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { ModeToggle } from "@/components/ModeToggle";
import { THEME_STORAGE_KEY, ThemeProvider } from "@/components/ThemeProvider";

beforeEach(() => {
  window.localStorage.clear();
  document.documentElement.classList.remove("dark");
  window.HTMLElement.prototype.hasPointerCapture = vi.fn();
  window.HTMLElement.prototype.releasePointerCapture = vi.fn();
  window.HTMLElement.prototype.scrollIntoView = vi.fn();
  globalThis.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
});

function openMenu() {
  // jsdom has no PointerEvent, so open the dropdown via the keyboard path.
  fireEvent.keyDown(screen.getByRole("button", { name: "Toggle theme" }), { key: "Enter" });
}

describe("ModeToggle", () => {
  it("sets the theme from the dropdown", () => {
    render(
      <ThemeProvider>
        <ModeToggle />
      </ThemeProvider>,
    );

    openMenu();
    fireEvent.click(screen.getByRole("menuitem", { name: "Dark" }));

    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });

  it("can switch back to light", () => {
    window.localStorage.setItem(THEME_STORAGE_KEY, "dark");
    render(
      <ThemeProvider>
        <ModeToggle />
      </ThemeProvider>,
    );

    openMenu();
    fireEvent.click(screen.getByRole("menuitem", { name: "Light" }));

    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe("light");
    expect(document.documentElement.classList.contains("dark")).toBe(false);
  });
});
