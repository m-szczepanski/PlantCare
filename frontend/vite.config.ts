import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api/health": {
        target: "http://localhost:5001",
        rewrite: () => "/health",
      },
      "/api": {
        target: "http://localhost:5001",
      },
    },
  },
});
