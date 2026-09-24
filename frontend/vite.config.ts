import react from '@vitejs/plugin-react'
import { defineConfig, type ProxyOptions } from 'vite'

const BACKEND_URL = 'http://localhost:5116'

// Local backend for relative /api and /hubs URLs (see src/config.ts). Also used
// by `npm run preview`, so the production bundle can be checked locally.
const backendProxy: Record<string, ProxyOptions> = {
  '/api': {
    target: BACKEND_URL,
    changeOrigin: true,
  },
  '/hubs': {
    target: BACKEND_URL,
    changeOrigin: true,
    ws: true,
  },
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: backendProxy,
  },
  preview: {
    proxy: backendProxy,
  },
})
