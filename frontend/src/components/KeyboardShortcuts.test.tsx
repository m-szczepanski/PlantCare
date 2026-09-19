import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { KeyboardShortcuts } from "@/components/KeyboardShortcuts";

function Harness({ onWater }: { onWater: () => void }) {
  const location = useLocation();
  return (
    <>
      <KeyboardShortcuts />
      <div data-testid="path">{location.pathname}</div>
      <input id="plant-search" aria-label="Search plants" />
      <div data-plant-card={1} tabIndex={0}>
        <button data-water-button onClick={onWater}>
          Water
        </button>
      </div>
    </>
  );
}

function renderHarness(onWater = vi.fn()) {
  render(
    <MemoryRouter initialEntries={["/plants"]}>
      <Routes>
        <Route path="/plants" element={<Harness onWater={onWater} />} />
        <Route path="/plants/new" element={<div>new plant page</div>} />
      </Routes>
    </MemoryRouter>,
  );
  return onWater;
}

describe("KeyboardShortcuts", () => {
  it("n navigates to the new plant page", () => {
    renderHarness();
    expect(screen.getByTestId("path")).toHaveTextContent("/plants");

    fireEvent.keyDown(document, { key: "n" });

    expect(screen.getByText("new plant page")).toBeInTheDocument();
  });

  it("/ focuses the plant search box", () => {
    renderHarness();

    fireEvent.keyDown(document, { key: "/" });

    expect(screen.getByLabelText("Search plants")).toHaveFocus();
  });

  it("w clicks the water button of the focused card", () => {
    const onWater = renderHarness();
    (screen.getByText("Water").parentElement as HTMLElement).focus();

    fireEvent.keyDown(document, { key: "w" });

    expect(onWater).toHaveBeenCalledTimes(1);
  });

  it("does not fire while typing in a field", () => {
    renderHarness();
    const search = screen.getByLabelText("Search plants");
    search.focus();

    fireEvent.keyDown(search, { key: "n" });

    expect(screen.getByTestId("path")).toHaveTextContent("/plants");
  });

  it("ignores modified keys", () => {
    renderHarness();

    fireEvent.keyDown(document, { key: "n", metaKey: true });

    expect(screen.getByTestId("path")).toHaveTextContent("/plants");
  });
});
