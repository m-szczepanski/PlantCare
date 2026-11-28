import { describe, expect, it } from "vitest";
import { MonthlyBarChart } from "@/components/charts/MonthlyBarChart";
import { renderWithProviders } from "@/test/render";

function months(count: number) {
  return Array.from({ length: count }, (_, index) => ({
    key: `2026-${String(index + 1).padStart(2, "0")}`,
    label: `Month ${index + 1}`,
    shortLabel: `M${index + 1}`,
    count: index + 1,
  }));
}

describe("MonthlyBarChart", () => {
  it("scrolls horizontally instead of squishing columns on narrow screens", () => {
    const { container } = renderWithProviders(
      <MonthlyBarChart data={months(12)} ariaLabel="Monthly waterings" className="h-32" />,
    );

    const scroller = container.querySelector('[aria-label="Monthly waterings"] > div');
    expect(scroller).toHaveClass("overflow-x-auto");
    expect(scroller!.firstElementChild).toHaveClass("flex", "items-end", "h-32");
  });

  it("gives every bar column a readable minimum width", () => {
    const { container } = renderWithProviders(
      <MonthlyBarChart data={months(12)} ariaLabel="Monthly waterings" className="h-32" />,
    );

    const columns = container.querySelectorAll(".overflow-x-auto > div > div");
    expect(columns).toHaveLength(12);
    for (const column of columns) {
      expect(column).toHaveClass("min-w-10", "flex-1");
    }
  });
});
