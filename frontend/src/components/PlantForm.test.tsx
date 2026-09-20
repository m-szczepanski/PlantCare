import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen } from "@testing-library/react";
import { plantProfilesApi } from "@/api/client";
import type { PlantProfile } from "@/api/types";
import { PlantForm } from "@/components/PlantForm";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  plantProfilesApi: { list: vi.fn() },
  roomsApi: { list: vi.fn().mockResolvedValue([]), create: vi.fn(), update: vi.fn(), remove: vi.fn() },
  ApiError: class ApiError extends Error {},
}));

const profiles: PlantProfile[] = [
  {
    id: 1,
    commonName: "Monstera",
    scientificName: null,
    defaultWateringIntervalDays: 7,
    lightRequirement: "Bright",
    humidityNotes: "Loves misting.",
    careTips: "Feed monthly.",
    toxicToPets: false,
    toxicToChildren: false,
    diagnosisChecklist: null,
    plantCount: 0,
  },
  {
    id: 2,
    commonName: "Pothos",
    scientificName: null,
    defaultWateringIntervalDays: 10,
    lightRequirement: "Medium",
    humidityNotes: "Average.",
    careTips: "Tolerant.",
    toxicToPets: false,
    toxicToChildren: false,
    diagnosisChecklist: null,
    plantCount: 0,
  },
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

      fireEvent.click(await screen.findByRole("combobox", { name: "Species profile" }));
      fireEvent.change(screen.getByPlaceholderText("Search profiles..."), { target: { value: "Poth" } });

      expect(screen.queryByText("Monstera (7d)")).not.toBeInTheDocument();
      fireEvent.click(await screen.findByRole("option", { name: "Pothos (10d)" }));

      expect(screen.getByRole("combobox", { name: "Species profile" })).toHaveTextContent("Pothos (10d)");
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

  it("previews the picked photo and passes it to onSubmit", async () => {
    const onSubmit = vi.fn();
    const { container } = renderWithProviders(
      <PlantForm submitting={false} submitLabel="Create plant" onSubmit={onSubmit} onCancel={vi.fn()} />,
    );

    const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement;
    const file = new File(["fake"], "leaf.png", { type: "image/png" });
    fireEvent.change(fileInput, { target: { files: [file] } });

    expect(await screen.findByRole("button", { name: "Change photo" })).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("Nick name"), { target: { value: "Pat" } });
    fireEvent.click(screen.getByRole("button", { name: "Create plant" }));

    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ nickName: "Pat" }), file);
  });

  it("rejects a too-large photo without setting it", async () => {
    const onSubmit = vi.fn();
    const { container } = renderWithProviders(
      <PlantForm submitting={false} submitLabel="Create plant" onSubmit={onSubmit} onCancel={vi.fn()} />,
    );

    const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement;
    const big = new File(["x".repeat(5 * 1024 * 1024 + 1)], "big.jpg", { type: "image/jpeg" });
    fireEvent.change(fileInput, { target: { files: [big] } });

    expect(await screen.findByRole("alert")).toHaveTextContent("larger than 5 MB");

    fireEvent.change(screen.getByLabelText("Nick name"), { target: { value: "Pat" } });
    fireEvent.click(screen.getByRole("button", { name: "Create plant" }));
    expect(onSubmit).toHaveBeenCalledWith(expect.anything(), null);
  });

  it("rejects a non-image file", async () => {
    const { container } = renderWithProviders(
      <PlantForm submitting={false} submitLabel="Create plant" onSubmit={vi.fn()} onCancel={vi.fn()} />,
    );

    const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement;
    const doc = new File(["x"], "notes.pdf", { type: "application/pdf" });
    fireEvent.change(fileInput, { target: { files: [doc] } });

    expect(await screen.findByRole("alert")).toHaveTextContent("Unsupported photo type");
  });
});
