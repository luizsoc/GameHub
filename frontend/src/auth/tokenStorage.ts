const TOKEN_KEY = 'gamehub.token'

// Single place that knows where the JWT is stored.
export function getToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY)
  } catch {
    return null
  }
}
