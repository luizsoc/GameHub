import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

const BACKEND_URL = 'http://localhost:5116'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: BACKEND_URL,
        changeOrigin: true,
      },
      '/hubs': {
        target: BACKEND_URL,
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
