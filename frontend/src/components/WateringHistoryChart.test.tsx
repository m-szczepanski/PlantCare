import { describe, expect, it } from "vitest";
import { render, screen, within } from "@testing-library/react";
import type { WateringLogEntry } from "@/api/types";
import { WateringHistoryChart } from "@/components/WateringHistoryChart";

const DAY_MS = 86_400_000;

function log(daysAgo: number, id = daysAgo): WateringLogEntry {
  return {
    id,
    wateredAt: new Date(Date.now() - daysAgo * DAY_MS).toISOString().replace("Z", ""),
    note: null,
    amountMilliliters: null,
    method: null,
  };
}

function monthLabel(date: Date): string {
  return date.toLocaleDateString(undefined, { month: "short", year: "numeric" });
}

describe("WateringHistoryChart", () => {
  it("renders one bar per month for the last six months", () => {
    render(<WateringHistoryChart logs={[log(1)]} />);

    const figure = screen.getByRole("figure");
    expect(figure).toHaveAccessibleName("Waterings per month over the last 6 months");
    expect(figure.querySelectorAll("[title]")).toHaveLength(6);
  });

  it("counts waterings per month and exposes the data to assistive tech", () => {
    const logs = [log(1), log(2, 11), log(40, 21)];
    render(<WateringHistoryChart logs={logs} />);

    const thisMonth = monthLabel(new Date());
    const watered40 = new Date(Date.now() - 40 * DAY_MS);
    const lastMonth = monthLabel(watered40);

    const rows = screen.getAllByRole("row");
    expect(rows).toHaveLength(7);
    const cellCount = (label: string) => {
      const row = rows.find((candidate) => candidate.textContent?.includes(label));
      expect(row).toBeDefined();
      return within(row!).getAllByRole("cell")[1].textContent;
    };
    expect(cellCount(thisMonth)).toBe("2");
    expect(cellCount(lastMonth)).toBe("1");
  });

  it("hides the chart when there are no waterings in view", () => {
    const { container } = render(<WateringHistoryChart logs={[log(400)]} />);

    expect(container).toBeEmptyDOMElement();
  });
});
