/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// https://vite.dev/config/

import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite"
import { viteEnvs } from "vite-envs";

export default defineConfig({
 base: "/",
 plugins: [react(), viteEnvs(), tailwindcss()],
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