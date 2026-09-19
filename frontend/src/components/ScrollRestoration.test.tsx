import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useNavigate } from "react-router-dom";
import { ScrollRestoration } from "@/components/ScrollRestoration";

let scrollY = 0;

function PageB() {
  const navigate = useNavigate();
  return <button onClick={() => navigate("/c")}>to c</button>;
}

function PageC() {
  const navigate = useNavigate();
  return <button onClick={() => navigate(-1)}>back</button>;
}

beforeEach(() => {
  scrollY = 0;
  Object.defineProperty(window, "scrollY", { configurable: true, get: () => scrollY });
  window.scrollTo = vi.fn();
});

describe("ScrollRestoration", () => {
  it("saves the outgoing scroll position and restores it when coming back", () => {
    render(
      <MemoryRouter initialEntries={["/b"]}>
        <ScrollRestoration />
        <Routes>
          <Route path="/b" element={<PageB />} />
          <Route path="/c" element={<PageC />} />
        </Routes>
      </MemoryRouter>,
    );

    scrollY = 240;
    fireEvent.click(screen.getByRole("button", { name: "to c" }));

    scrollY = 0;
    fireEvent.click(screen.getByRole("button", { name: "back" }));

    expect(window.scrollTo).toHaveBeenCalledWith(0, 240);
  });

  it("does not scroll on a first visit", () => {
    render(
      <MemoryRouter initialEntries={["/b"]}>
        <ScrollRestoration />
        <Routes>
          <Route path="/b" element={<PageB />} />
        </Routes>
      </MemoryRouter>,
    );

    expect(window.scrollTo).not.toHaveBeenCalled();
  });
});
