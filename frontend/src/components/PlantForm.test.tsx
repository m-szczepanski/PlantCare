import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen } from "@testing-library/react";
import { plantProfilesApi } from "@/api/client";
import type { Plant, PlantProfile } from "@/api/types";
import { PlantForm } from "@/components/PlantForm";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  plantProfilesApi: { list: vi.fn() },
  roomsApi: { list: vi.fn().mockResolvedValue([]), create: vi.fn(), update: vi.fn(), remove: vi.fn() },
  referenceDataApi: {
    soilTypes: vi.fn().mockResolvedValue([
      { type: "AllPurpose", wateringIntervalFactor: 1 },
      { type: "CactusMix", wateringIntervalFactor: 0.7 },
      { type: "ChunkyBark", wateringIntervalFactor: 0.55 },
      { type: "PeatCoco", wateringIntervalFactor: 1.15 },
      { type: "SemiHydro", wateringIntervalFactor: 1.3 },
      { type: "SelfWatering", wateringIntervalFactor: 1.5 },
    ]),
    soilMixes: vi.fn().mockResolvedValue([
      { id: 1, name: "Aroid chunky blend" },
      { id: 2, name: "Cactus & succulent mix" },
    ]),
  },
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

const basePlant: Plant = {
  id: 1,
  nickName: "Pothos Pat",
  roomId: null,
  roomName: null,
  photoUrl: null,
  potSizeCm: null,
  soilType: null,
  soilMix: null,
  propagatedFrom: null,
  notifyEnabled: true,
  snoozedUntil: null,
  soilWetUntil: null,
  acquiredDate: "2026-01-01T00:00:00",
  plantProfileId: null,
  profileCommonName: null,
  profileToxicToPets: false,
  profileToxicToChildren: false,
  careTips: null,
  customWateringIntervalDays: 10,
  reduceInWinter: null,
  lastWateredAt: null,
  dueStatus: "Upcoming",
  wateringIntervalDays: 10,
  daysUntilDue: 5,
  nextDueDate: null,
  dueMessage: "5 days until due",
  roomLightMatch: null,
};

function renderFormWithInitial(over: Partial<Plant>) {
  return renderWithProviders(
    <PlantForm
      initial={{ ...basePlant, ...over }}
      submitting={false}
      submitLabel="Update details"
      onSubmit={vi.fn()}
      onCancel={vi.fn()}
    />,
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

  it("applies the soil permeability factor to the live next-due preview", async () => {
    renderFormWithInitial({ soilType: "SemiHydro" });

    // The hint only renders once the soil options have loaded from the API.
    const hint = await screen.findByText("Semi-hydroton (LECA) changes watering from 10 to 13 days");
    expect(hint).toBeInTheDocument();
    // 10-day base scaled by the 1.3 semi-hydro factor -> 13 days.
    expect(screen.getByRole("status")).toHaveTextContent("in 13 days");
  });

  it("keeps all-purpose soil on the base interval (no hint)", async () => {
    renderFormWithInitial({ soilType: "AllPurpose" });

    const status = await screen.findByRole("status");
    expect(status).toHaveTextContent("in 10 days");
    expect(screen.queryByText(/changes watering/)).not.toBeInTheDocument();
  });

  it("passes the selected soil type through to onSubmit", async () => {
    const onSubmit = vi.fn();
    renderWithProviders(
      <PlantForm
        initial={{ ...basePlant, soilType: "SemiHydro" }}
        submitting={false}
        submitLabel="Update details"
        onSubmit={onSubmit}
        onCancel={vi.fn()}
      />,
    );

    fireEvent.click(await screen.findByRole("button", { name: "Update details" }));

    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ soilType: "SemiHydro", customWateringIntervalDays: 10 }),
      null,
    );
  });

  it("passes the selected soil mix through to onSubmit", async () => {
    const onSubmit = vi.fn();
    const { container } = renderWithProviders(
      <PlantForm
        submitting={false}
        submitLabel="Create plant"
        onSubmit={onSubmit}
        onCancel={vi.fn()}
      />,
    );

    // The catalog is fetched from the DB and offered as a single-choice picker.
    const trigger = await screen.findByRole("combobox", { name: "Soil mix" });
    expect(container.querySelector('input#soilMix')).toBeNull();
    fireEvent.click(trigger);
    fireEvent.click(await screen.findByRole("option", { name: "Cactus & succulent mix" }));

    fireEvent.change(screen.getByLabelText("Nick name"), { target: { value: "Pat" } });
    fireEvent.click(screen.getByRole("button", { name: "Create plant" }));

    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ soilMix: "Cactus & succulent mix" }),
      null,
    );
  });

  it("preserves a legacy free-text soil mix not present in the catalog", async () => {
    const onSubmit = vi.fn();
    renderWithProviders(
      <PlantForm
        initial={{ ...basePlant, soilMix: "Grandma's secret blend" }}
        submitting={false}
        submitLabel="Update details"
        onSubmit={onSubmit}
        onCancel={vi.fn()}
      />,
    );

    const trigger = await screen.findByRole("combobox", { name: "Soil mix" });
    expect(trigger).toHaveTextContent("Grandma's secret blend");

    fireEvent.click(screen.getByRole("button", { name: "Update details" }));
    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ soilMix: "Grandma's secret blend" }),
      null,
    );
  });
});
