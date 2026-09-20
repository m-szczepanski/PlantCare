import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { plantProfilesApi } from "@/api/client";
import type { PlantProfile } from "@/api/types";
import ProfilesPage from "@/pages/ProfilesPage";
import { renderWithProviders } from "@/test/render";

vi.mock("@/api/client", () => ({
  plantProfilesApi: { list: vi.fn(), create: vi.fn(), update: vi.fn() },
  ApiError: class ApiError extends Error {},
}));

const monstera: PlantProfile = {
  id: 1,
  commonName: "Monstera",
  scientificName: "Monstera deliciosa",
  defaultWateringIntervalDays: 7,
  lightRequirement: "Bright",
  humidityNotes: "Loves misting.",
  careTips: "Feed monthly.",
  toxicToPets: false,
  toxicToChildren: false,
  plantCount: 2,
};

beforeEach(() => {
  vi.mocked(plantProfilesApi.list).mockReset().mockResolvedValue([monstera]);
  vi.mocked(plantProfilesApi.create).mockReset();
  vi.mocked(plantProfilesApi.update).mockReset();
});

describe("ProfilesPage", () => {
  it("lists profiles with their metadata", async () => {
    renderWithProviders(<ProfilesPage />, { route: "/profiles" });

    expect(await screen.findByText("Monstera")).toBeInTheDocument();
    expect(screen.getByText("Monstera deliciosa")).toBeInTheDocument();
    expect(screen.getByText("7d water")).toBeInTheDocument();
    expect(screen.getByText("2 plants")).toBeInTheDocument();
  });

  it("creates a profile through the API", async () => {
    vi.mocked(plantProfilesApi.create).mockResolvedValue({ ...monstera, id: 2, commonName: "ZZ plant", plantCount: 0 });

    renderWithProviders(<ProfilesPage />, { route: "/profiles" });

    fireEvent.click(await screen.findByRole("button", { name: "New profile" }));
    fireEvent.change(screen.getByLabelText("Common name"), { target: { value: "ZZ plant" } });
    fireEvent.click(screen.getByRole("button", { name: "Create profile" }));

    await waitFor(() => expect(plantProfilesApi.create).toHaveBeenCalledTimes(1));
    const input = vi.mocked(plantProfilesApi.create).mock.calls[0][0];
    expect(input.commonName).toBe("ZZ plant");
    expect(input.defaultWateringIntervalDays).toBe(7);
  });

  it("edits an existing profile", async () => {
    vi.mocked(plantProfilesApi.update).mockResolvedValue({ ...monstera, humidityNotes: "Average room air." });

    renderWithProviders(<ProfilesPage />, { route: "/profiles" });

    fireEvent.click(await screen.findByRole("button", { name: "Edit" }));
    expect(screen.getByLabelText("Common name")).toHaveValue("Monstera");

    fireEvent.change(screen.getByLabelText("Humidity notes"), { target: { value: "Average room air." } });
    fireEvent.click(screen.getByRole("button", { name: "Save changes" }));

    await waitFor(() =>
      expect(plantProfilesApi.update).toHaveBeenCalledWith(1, expect.objectContaining({
        commonName: "Monstera",
        humidityNotes: "Average room air.",
      })),
    );
  });
});
