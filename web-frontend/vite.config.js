// https://vite.dev/config/

import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { viteEnvs } from "vite-envs";

export default defineConfig({
 base: "/",
 plugins: [react(), viteEnvs()],
 preview: {
  port: 5173,
  strictPort: true,
 },
 server: {
  port: 5173,
  strictPort: true,
  host: true,
  origin: "http://0.0.0.0:5173",
 },
 build: {
  rollupOptions: {
   // Force JS-only rollup to avoid native binary issues
   external: [],
   output: {
    manualChunks: undefined,
   },
  },
 },
});