import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen } from "@testing-library/react";
import { plantProfilesApi } from "@/api/client";
import type { PlantProfileOption } from "@/api/types";
import { PlantForm } from "@/components/PlantForm";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  plantProfilesApi: { list: vi.fn() },
  ApiError: class ApiError extends Error {},
}));

const profiles: PlantProfileOption[] = [
  { id: 1, commonName: "Monstera", scientificName: null, defaultWateringIntervalDays: 7 },
  { id: 2, commonName: "Pothos", scientificName: null, defaultWateringIntervalDays: 10 },
];

function renderForm() {
  return renderWithProviders(
    <PlantForm submitting={false} submitLabel="Create plant" onSubmit={vi.fn()} onCancel={vi.fn()} />,
  );
}

beforeEach(() => {
  vi.mocked(plantProfilesApi.list).mockReset().mockResolvedValue(profiles);
});

describe("PlantForm", () => {
  it(
    "shows the live next-due preview from a searched profile selection",
    async () => {
      renderForm();

      fireEvent.click(await screen.findByRole("combobox"));
      fireEvent.change(screen.getByPlaceholderText("Search profiles..."), { target: { value: "Poth" } });

      expect(screen.queryByText("Monstera (7d)")).not.toBeInTheDocument();
      fireEvent.click(await screen.findByRole("option", { name: "Pothos (10d)" }));

      expect(screen.getByRole("combobox")).toHaveTextContent("Pothos (10d)");
      expect(screen.getByRole("status")).toHaveTextContent("Next watering due");
      expect(screen.getByRole("status")).toHaveTextContent("in 10 days");
      expect(screen.getByPlaceholderText("Profile default: 10")).toBeInTheDocument();
    },
    // cmdk list rendering is slow under jsdom
    30_000,
  );

  it("previews a manually entered custom interval", async () => {
    renderForm();

    expect(await screen.findByRole("status")).toHaveTextContent("No watering schedule");

    fireEvent.change(screen.getByLabelText("Custom interval (days)"), { target: { value: "5" } });

    expect(screen.getByRole("status")).toHaveTextContent("in 5 days");
  });

  it("renders inline field errors from API validation responses", async () => {
    renderWithProviders(
      <PlantForm
        submitting={false}
        fieldErrors={{ nickName: "The NickName field is required." }}
        submitLabel="Create plant"
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
      />,
    );

    expect(await screen.findByText("The NickName field is required.")).toBeInTheDocument();
    expect(screen.getByLabelText("Nick name")).toHaveAttribute("aria-invalid", "true");
  });
});
