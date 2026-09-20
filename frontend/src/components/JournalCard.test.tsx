import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { plantsApi } from "@/api/client";
import type { JournalEntry } from "@/api/types";
import JournalCard from "@/components/JournalCard";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  plantsApi: {
    journal: vi.fn(),
    addJournalEntry: vi.fn(),
    deleteJournalEntry: vi.fn(),
  },
  ApiError: class ApiError extends Error {},
}));

const entries: JournalEntry[] = [
  { id: 2, entryDate: "2026-06-01T00:00:00", photoUrl: "/uploads/plants/1/journal/b.png", text: "Big leaves now" },
  { id: 1, entryDate: "2026-03-01T00:00:00", photoUrl: null, text: "Small cutting" },
];

beforeEach(() => {
  vi.mocked(plantsApi.journal).mockReset().mockResolvedValue(entries);
  vi.mocked(plantsApi.addJournalEntry).mockReset();
  vi.mocked(plantsApi.deleteJournalEntry).mockReset().mockResolvedValue(undefined);
});

describe("JournalCard", () => {
  it("lists entries newest first with a before/after comparison", async () => {
    renderWithProviders(<JournalCard plantId={1} nickName="Monstera Mike" />);

    expect(await screen.findByText("Big leaves now")).toBeInTheDocument();
    expect(screen.getByText("Small cutting")).toBeInTheDocument();
    expect(screen.getByText("Before / after")).toBeInTheDocument();
    expect(screen.getByText(/^Before · /)).toBeInTheDocument();
    expect(screen.getByText(/^After · /)).toBeInTheDocument();
  });

  it("submits a new entry with date, note and file", async () => {
    vi.mocked(plantsApi.addJournalEntry).mockResolvedValue(entries[0]);

    renderWithProviders(<JournalCard plantId={1} nickName="Monstera Mike" />);

    await screen.findByText("Big leaves now");
    fireEvent.change(screen.getByLabelText("Note"), { target: { value: "Flower spike" } });

    fireEvent.click(screen.getByRole("button", { name: "Add entry" }));

    await waitFor(() =>
      expect(plantsApi.addJournalEntry).toHaveBeenCalledWith(
        1,
        expect.objectContaining({ text: "Flower spike" }),
      ),
    );
  });

  it("deletes an entry", async () => {
    renderWithProviders(<JournalCard plantId={1} nickName="Monstera Mike" />);

    await screen.findByText("Small cutting");
    fireEvent.click(screen.getAllByRole("button", { name: "Delete" })[1]);

    await waitFor(() => expect(plantsApi.deleteJournalEntry).toHaveBeenCalledWith(1, 1));
  });
});
