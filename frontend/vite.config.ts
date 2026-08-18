import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { TanStackRouterVite } from '@tanstack/router-plugin/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [TanStackRouterVite(), react(), tailwindcss()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5216',
        changeOrigin: true,
      },
      '/hubs': {
        target: 'http://localhost:5216',
        ws: true,
      },
    },
  },
})
