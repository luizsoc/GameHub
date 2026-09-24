import type { HubConnection } from '@microsoft/signalr'
import { useEffect, useState } from 'react'
import { getToken } from '../auth/tokenStorage'
import { createChatConnection } from '../services/chatConnection'

export type ChatConnectionStatus =
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'disconnected'

export interface ChatConnectionState {
  // Set while the connection is usable or recovering; null otherwise.
  connection: HubConnection | null
  status: ChatConnectionStatus
  error: string | null
}

// Owns the single SignalR connection. It lives exactly as long as the
// component using it (MainLayout, only rendered for authenticated users):
// logout unmounts it, which stops the connection and any reconnect attempts.
export function useChatConnection(): ChatConnectionState {
  const [state, setState] = useState<ChatConnectionState>(() => ({
    connection: null,
    status: getToken() ? 'connecting' : 'disconnected',
    error: null,
  }))

  useEffect(() => {
    // Without a token there is no authenticated connection to open;
    // the existing auth flow takes the user back to /login.
    if (!getToken()) {
      return
    }

    const connection = createChatConnection()
    let disposed = false

    // Events that arrive after cleanup (e.g. onclose caused by our own
    // stop() on logout) must not touch state.
    function update(next: ChatConnectionState) {
      if (!disposed) {
        setState(next)
      }
    }

    connection.onreconnecting(() =>
      update({ connection, status: 'reconnecting', error: null }),
    )
    connection.onreconnected(() =>
      update({ connection, status: 'connected', error: null }),
    )
    // Reached when automatic reconnection gives up (or the server closes).
    connection.onclose(() =>
      update({
        connection: null,
        status: 'disconnected',
        error: 'A conexão com o chat foi perdida.',
      }),
    )

    // StrictMode (dev) mounts, unmounts and remounts effects synchronously.
    // Starting in a microtask means the discarded first instance never opens
    // a connection, so there is only ever one live connection.
    queueMicrotask(() => {
      if (disposed) {
        return
      }

      connection
        .start()
        .then(() => update({ connection, status: 'connected', error: null }))
        .catch(() =>
          update({
            connection: null,
            status: 'disconnected',
            error: 'Não foi possível conectar ao chat.',
          }),
        )
    })

    return () => {
      disposed = true
      connection.stop().catch(() => {
        // Nothing left to clean up if stopping fails.
      })
    }
  }, [])

  return state
}
