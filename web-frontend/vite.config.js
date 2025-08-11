// https://vite.dev/config/

import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  // Check if we're in hotcontainer mode (behind load balancer)
  const isHotContainer = process.env.VITE_MODE === 'hotcontainer';
  
  return {
    base: "/",
    plugins: [react()],
    preview: {
      port: 8090,
      strictPort: true,
    },
    server: {
      port: 8090,
      strictPort: true,
      host: true,
      origin: "http://0.0.0.0:8090",
      // Only proxy in hotcontainer mode
      ...(isHotContainer && {
        proxy: {
          '/api': {
            target: 'http://localhost:8080',
            changeOrigin: true
          }
        }
      })
    }
  }
});
