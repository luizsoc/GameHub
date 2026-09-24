import axios from 'axios'
import { getToken } from '../auth/tokenStorage'

// Requests go through the Vite dev proxy (see vite.config.ts).
export const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use((config) => {
  const token = getToken()

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})
