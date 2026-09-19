import { readFileSync, existsSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";

const PUBLIC_DIR = join(process.cwd(), "public");

function loadManifest() {
  return JSON.parse(readFileSync(join(PUBLIC_DIR, "manifest.webmanifest"), "utf8"));
}

describe("PWA shell", () => {
  it("declares a standalone manifest with the required fields", () => {
    const manifest = loadManifest();

    expect(manifest.name).toBe("PlantCare");
    expect(manifest.start_url).toBe("/");
    expect(manifest.scope).toBe("/");
    expect(manifest.display).toBe("standalone");
    expect(manifest.theme_color).toBe("#16a34a");
  });

  it("ships every icon referenced by the manifest", () => {
    const manifest = loadManifest();
    const sizes = manifest.icons.map((icon: { sizes: string }) => icon.sizes);

    expect(sizes).toContain("192x192");
    expect(sizes).toContain("512x512");
    expect(manifest.icons.some((icon: { purpose?: string }) => icon.purpose === "maskable")).toBe(
      true,
    );
    for (const icon of manifest.icons) {
      expect(existsSync(join(PUBLIC_DIR, icon.src.replace(/^\//, "")))).toBe(true);
    }
  });

  it("links the manifest, favicon, and theme color from index.html", () => {
    const html = readFileSync(join(process.cwd(), "index.html"), "utf8");

    expect(html).toContain('<link rel="manifest" href="/manifest.webmanifest"');
    expect(html).toContain('<link rel="icon" href="/favicon.svg"');
    expect(html).toContain('<link rel="apple-touch-icon"');
    expect(html).toContain('name="theme-color"');
  });

  it("serves the manifest with a JSON mime type from nginx", () => {
    const nginx = readFileSync(join(process.cwd(), "nginx.conf"), "utf8");

    expect(nginx).toContain("application/manifest+json");
  });
});
