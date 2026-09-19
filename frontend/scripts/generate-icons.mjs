import { deflateSync } from "node:zlib";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..");

const BG = [22, 163, 74, 255];
const LEAF = [240, 253, 244, 255];
const TRANSPARENT = [0, 0, 0, 0];

const SQRT1_2 = Math.SQRT1_2;

function leafColor(u, v, scale) {
  const p = ((u + v) * SQRT1_2) / scale;
  const q = ((v - u) * SQRT1_2) / scale;
  const A = 0.293;
  const B = 0.152;
  const t = (p / A) ** 2 + (q / B) ** 2;
  if (t <= 1) {
    return Math.abs(q) <= 0.012 && Math.abs(p) <= 0.27 ? BG : LEAF;
  }
  return null;
}

function insideRoundedSquare(x, y, radius) {
  const cx = Math.min(Math.max(x, radius), 1 - radius);
  const cy = Math.min(Math.max(y, radius), 1 - radius);
  return (x - cx) ** 2 + (y - cy) ** 2 <= radius ** 2;
}

function renderIcon(size, { rounded, scale }) {
  const pixels = Buffer.alloc(size * size * 4);
  const step = 1 / 3;
  for (let y = 0; y < size; y += 1) {
    for (let x = 0; x < size; x += 1) {
      let r = 0;
      let g = 0;
      let b = 0;
      let a = 0;
      for (let sy = 0; sy < 3; sy += 1) {
        for (let sx = 0; sx < 3; sx += 1) {
          const fx = (x + (sx + 0.5) * step) / size;
          const fy = (y + (sy + 0.5) * step) / size;
          const inShape = rounded ? insideRoundedSquare(fx, fy, 0.22) : true;
          let color;
          if (!inShape) {
            color = TRANSPARENT;
          } else {
            color = leafColor(fx - 0.5, fy - 0.5, scale) ?? BG;
          }
          r += color[0];
          g += color[1];
          b += color[2];
          a += color[3];
        }
      }
      const offset = (y * size + x) * 4;
      pixels[offset] = Math.round(r / 9);
      pixels[offset + 1] = Math.round(g / 9);
      pixels[offset + 2] = Math.round(b / 9);
      pixels[offset + 3] = Math.round(a / 9);
    }
  }
  return pixels;
}

const CRC_TABLE = new Uint32Array(256).map((_, n) => {
  let c = n;
  for (let k = 0; k < 8; k += 1) {
    c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  }
  return c >>> 0;
});

function crc32(bytes) {
  let c = 0xffffffff;
  for (const byte of bytes) {
    c = CRC_TABLE[(c ^ byte) & 0xff] ^ (c >>> 8);
  }
  return (c ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const length = Buffer.alloc(4);
  length.writeUInt32BE(data.length);
  const body = Buffer.concat([Buffer.from(type, "ascii"), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(body));
  return Buffer.concat([length, body, crc]);
}

function encodePng(size, rgba) {
  const raw = Buffer.alloc(size * (size * 4 + 1));
  for (let y = 0; y < size; y += 1) {
    raw[y * (size * 4 + 1)] = 0;
    rgba.copy(raw, y * (size * 4 + 1) + 1, y * size * 4, (y + 1) * size * 4);
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(size, 0);
  ihdr.writeUInt32BE(size, 4);
  ihdr[8] = 8;
  ihdr[9] = 6;
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk("IHDR", ihdr),
    chunk("IDAT", deflateSync(raw, { level: 9 })),
    chunk("IEND", Buffer.alloc(0)),
  ]);
}

const icons = [
  { file: "public/icons/icon-512.png", size: 512, rounded: true, scale: 1 },
  { file: "public/icons/icon-192.png", size: 192, rounded: true, scale: 1 },
  { file: "public/icons/favicon-32.png", size: 32, rounded: true, scale: 1 },
  { file: "public/icons/maskable-icon-512.png", size: 512, rounded: false, scale: 0.8 },
  { file: "public/icons/apple-touch-icon.png", size: 180, rounded: false, scale: 0.9 },
];

for (const icon of icons) {
  const out = join(ROOT, icon.file);
  mkdirSync(dirname(out), { recursive: true });
  writeFileSync(out, encodePng(icon.size, renderIcon(icon.size, icon)));
  console.log("wrote", icon.file);
}
