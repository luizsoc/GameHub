// Public, build-time configuration.
//
// Every VITE_* variable is embedded in the JavaScript bundle and readable by
// anyone who loads the page: never put secrets (JWT key, database password,
// private keys) in them.
//
// VITE_API_URL is the backend origin (e.g. https://api.example.com), only
// needed when the API is served from a different origin than the frontend.
// Left unset, the app uses relative URLs: the Vite proxy in development, or a
// reverse proxy that serves frontend and API on the same domain.
const apiOrigin = (import.meta.env.VITE_API_URL ?? '').trim().replace(/\/+$/, '')

export const API_BASE_URL = `${apiOrigin}/api`

export const CHAT_HUB_URL = `${apiOrigin}/hubs/chat`
