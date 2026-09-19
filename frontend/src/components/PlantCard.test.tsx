import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import type { ReactElement } from "react";
import { PlantCard } from "@/components/PlantCard";
import type { Plant } from "@/api/types";

function plant(over: Partial<Plant>): Plant {
  return {
    id: 1,
    nickName: "Monstera Mike",
    location: "Living room",
    photoUrl: null,
    acquiredDate: "2026-01-01T00:00:00",
    plantProfileId: null,
    profileCommonName: null,
    careTips: null,
    customWateringIntervalDays: 7,
    lastWateredAt: null,
    dueStatus: "Upcoming",
    wateringIntervalDays: 7,
    daysUntilDue: 4,
    nextDueDate: null,
    dueMessage: "4 days until due",
    ...over,
  };
}

function renderCard(el: ReactElement) {
  return render(<MemoryRouter>{el}</MemoryRouter>);
}

describe("PlantCard", () => {
  it("shows days until due as the primary number", () => {
    renderCard(<PlantCard plant={plant({})} />);

    expect(screen.getByText("4")).toBeInTheDocument();
    expect(screen.getByText("days to go")).toBeInTheDocument();
    expect(screen.getByText("Upcoming")).toBeInTheDocument();
  });

  it("uses singular units for one day", () => {
    renderCard(<PlantCard plant={plant({ dueStatus: "Overdue", daysUntilDue: -1 })} />);

    expect(screen.getByText("1")).toBeInTheDocument();
    expect(screen.getByText("day overdue")).toBeInTheDocument();
  });

  it("highlights overdue plants with the number of days overdue", () => {
    renderCard(
      <PlantCard
        plant={plant({
          dueStatus: "Overdue",
          daysUntilDue: -2,
          lastWateredAt: new Date(Date.now() - 86_400_000).toISOString(),
        })}
      />,
    );

    expect(screen.getByText("2")).toBeInTheDocument();
    expect(screen.getByText("days overdue")).toBeInTheDocument();
    expect(screen.getByText("Overdue")).toBeInTheDocument();
    expect(screen.getByText("Watered yesterday")).toBeInTheDocument();
  });

  it("shows 'Now / due today' for plants due today", () => {
    renderCard(<PlantCard plant={plant({ dueStatus: "DueToday", daysUntilDue: 0 })} />);

    expect(screen.getByText("Now")).toBeInTheDocument();
    expect(screen.getByText("due today")).toBeInTheDocument();
    expect(screen.getByText("Due today")).toBeInTheDocument();
  });

  it("shows a dash and 'not scheduled' for plants without a schedule", () => {
    renderCard(<PlantCard plant={plant({ dueStatus: "NotScheduled", daysUntilDue: null })} />);

    expect(screen.getByText("—")).toBeInTheDocument();
    expect(screen.getByText("not scheduled")).toBeInTheDocument();
    expect(screen.getByText("No schedule")).toBeInTheDocument();
  });

  it("renders the relative watered text", () => {
    renderCard(
      <PlantCard
        plant={plant({
          lastWateredAt: new Date(Date.now() - 3 * 86_400_000).toISOString(),
        })}
      />,
    );

    expect(screen.getByText("Watered 3 days ago")).toBeInTheDocument();
  });

  it("says not watered yet without a last watering", () => {
    renderCard(<PlantCard plant={plant({})} />);

    expect(screen.getByText("Not watered yet")).toBeInTheDocument();
  });

  it("shows an inline water button wired to the callback", () => {
    const onWater = vi.fn();
    const p = plant({ id: 7 });
    renderCard(<PlantCard plant={p} onWater={onWater} />);

    fireEvent.click(screen.getByRole("button", { name: "Water" }));
    expect(onWater).toHaveBeenCalledWith(p);
  });

  it("hides the water button while a watering is in flight", () => {
    renderCard(<PlantCard plant={plant({})} onWater={vi.fn()} isWatering />);

    expect(screen.getByRole("button", { name: "Watering..." })).toBeDisabled();
  });
});
