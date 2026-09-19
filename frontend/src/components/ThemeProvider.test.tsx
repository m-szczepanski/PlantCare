import { beforeEach, describe, expect, it, vi } from "vitest";
import { act, fireEvent, render, screen } from "@testing-library/react";
import {
  THEME_STORAGE_KEY,
  ThemeProvider,
  useTheme,
  type Theme,
} from "@/components/ThemeProvider";

function Probe() {
  const { theme, resolvedTheme, setTheme } = useTheme();
  return (
    <div>
      <span data-testid="theme">{theme}</span>
      <span data-testid="resolved">{resolvedTheme}</span>
      {(["light", "dark", "system"] as Theme[]).map((t) => (
        <button key={t} onClick={() => setTheme(t)}>
          set {t}
        </button>
      ))}
    </div>
  );
}

interface MqlMock {
  matches: boolean;
  media: string;
  onchange: null;
  listeners: Set<(e: { matches: boolean }) => void>;
  addEventListener: (_: string, cb: (e: { matches: boolean }) => void) => void;
  removeEventListener: (_: string, cb: (e: { matches: boolean }) => void) => void;
  dispatchEvent: () => boolean;
  setPrefersDark: (matches: boolean) => void;
}

function mockMatchMedia(prefersDark: boolean): MqlMock {
  const listeners = new Set<(e: { matches: boolean }) => void>();
  const mql: MqlMock = {
    matches: prefersDark,
    media: "(prefers-color-scheme: dark)",
    onchange: null,
    listeners,
    addEventListener: (_, cb) => listeners.add(cb),
    removeEventListener: (_, cb) => listeners.delete(cb),
    dispatchEvent: () => false,
    setPrefersDark: (next) => {
      mql.matches = next;
      listeners.forEach((cb) => cb({ matches: next }));
    },
  };
  vi.stubGlobal("matchMedia", () => mql);
  return mql;
}

beforeEach(() => {
  window.localStorage.clear();
  document.documentElement.classList.remove("dark");
  vi.unstubAllGlobals();
});

describe("ThemeProvider", () => {
  it("defaults to system and follows prefers-color-scheme", () => {
    const mql = mockMatchMedia(true);
    render(
      <ThemeProvider>
        <Probe />
      </ThemeProvider>,
    );

    expect(screen.getByTestId("theme")).toHaveTextContent("system");
    expect(screen.getByTestId("resolved")).toHaveTextContent("dark");
    expect(mql.listeners.size).toBe(1);
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });

  it("reacts to system preference changes", () => {
    const mql = mockMatchMedia(false);
    render(
      <ThemeProvider>
        <Probe />
      </ThemeProvider>,
    );

    expect(document.documentElement.classList.contains("dark")).toBe(false);
    act(() => mql.setPrefersDark(true));
    expect(screen.getByTestId("resolved")).toHaveTextContent("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });

  it("applies an explicit theme and persists it", () => {
    mockMatchMedia(false);
    render(
      <ThemeProvider>
        <Probe />
      </ThemeProvider>,
    );

    fireEvent.click(screen.getByText("set dark"));
    expect(screen.getByTestId("theme")).toHaveTextContent("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe("dark");

    fireEvent.click(screen.getByText("set light"));
    expect(document.documentElement.classList.contains("dark")).toBe(false);
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe("light");
  });

  it("restores the stored theme on mount", () => {
    mockMatchMedia(false);
    window.localStorage.setItem(THEME_STORAGE_KEY, "dark");
    render(
      <ThemeProvider>
        <Probe />
      </ThemeProvider>,
    );

    expect(screen.getByTestId("theme")).toHaveTextContent("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });

  it("ignores invalid stored values", () => {
    mockMatchMedia(false);
    window.localStorage.setItem(THEME_STORAGE_KEY, "neon");
    render(
      <ThemeProvider>
        <Probe />
      </ThemeProvider>,
    );

    expect(screen.getByTestId("theme")).toHaveTextContent("system");
    expect(document.documentElement.classList.contains("dark")).toBe(false);
  });
});
