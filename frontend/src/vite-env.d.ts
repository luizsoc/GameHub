// Typed public env variables (see src/config.ts). Only VITE_* values reach the
// bundle, and all of them are public.
interface ImportMetaEnv {
  readonly VITE_API_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
