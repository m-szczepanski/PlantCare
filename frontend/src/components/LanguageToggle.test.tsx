import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import i18n from "@/i18n";
import { LANGUAGE_STORAGE_KEY } from "@/i18n";
import { LanguageToggle } from "@/components/LanguageToggle";

beforeEach(() => {
  window.localStorage.clear();
  void i18n.changeLanguage("en");
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
  fireEvent.keyDown(screen.getByRole("button", { name: "Change language" }), { key: "Enter" });
}

describe("LanguageToggle", () => {
  it("switches the app to Polish", async () => {
    render(<LanguageToggle />);

    openMenu();
    fireEvent.click(screen.getByRole("menuitem", { name: "Polski" }));

    expect(window.localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe("pl");
    await vi.waitFor(() => {
      expect(i18n.language).toBe("pl");
      expect(document.documentElement.lang).toBe("pl");
    });
    void i18n.changeLanguage("en");
  });

  it("renders translated menu items", async () => {
    window.localStorage.setItem(LANGUAGE_STORAGE_KEY, "pl");
    await i18n.changeLanguage("pl");

    render(<LanguageToggle />);
    fireEvent.keyDown(screen.getByRole("button", { name: "Zmień język" }), { key: "Enter" });

    expect(screen.getByRole("menuitem", { name: "Polski" })).toBeInTheDocument();
    await i18n.changeLanguage("en");
  });
});
