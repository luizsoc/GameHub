const TOKEN_KEY = 'gamehub.token'

// Single place that knows where the JWT is stored.
// localStorage can throw (private mode, blocked storage), so every access is guarded.
export function getToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY)
  } catch {
    return null
  }
}

export function setToken(token: string): void {
  try {
    localStorage.setItem(TOKEN_KEY, token)
  } catch {
    // Storage unavailable: the session will only last until the page reloads.
  }
}

export function removeToken(): void {
  try {
    localStorage.removeItem(TOKEN_KEY)
  } catch {
    // Nothing to remove if storage is unavailable.
  }
}
