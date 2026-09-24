import axios, { isAxiosError } from 'axios'
import { notifyUnauthorized } from '../auth/authEvents'
import { getToken, removeToken } from '../auth/tokenStorage'

// Requests go through the Vite dev proxy (see vite.config.ts).
export const api = axios.create({
  baseURL: '/api',
  // A backend that accepts the connection but never answers would otherwise
  // leave every loading state spinning forever.
  timeout: 15_000,
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

// A 401 on a request that carried the *current* token means the session is
// no longer valid. The request is never retried, so this cannot loop.
// 401s on anonymous requests (e.g. invalid login credentials) are left alone,
// and so are 401s for a token that has already been replaced.
api.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (isAxiosError(error) && error.response?.status === 401) {
      const token = getToken()
      const sentAuthorization = error.config?.headers.Authorization

      if (token && sentAuthorization === `Bearer ${token}`) {
        removeToken()
        notifyUnauthorized()
      }
    }

    return Promise.reject(error)
  },
)
