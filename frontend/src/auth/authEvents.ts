// Lets the HTTP client signal "the session is no longer valid"
// without importing React/AuthContext (avoids a circular dependency).

type Listener = () => void

const unauthorizedListeners = new Set<Listener>()

export function onUnauthorized(listener: Listener): () => void {
  unauthorizedListeners.add(listener)

  return () => {
    unauthorizedListeners.delete(listener)
  }
}

export function notifyUnauthorized(): void {
  unauthorizedListeners.forEach((listener) => listener())
}
