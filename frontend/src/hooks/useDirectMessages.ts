import type { HubConnection } from '@microsoft/signalr'
import { useCallback, useEffect, useRef, useState } from 'react'
import * as directMessagesApi from '../api/directMessages'
import { getErrorMessage } from '../api/errors'
import type { ChatConnectionStatus } from './useChatConnection'
import { onDirectMessageCreated } from '../services/chatConnection'
import type { DirectMessageResponse } from '../types/channel'

// Same order as the backend list: by the other participant's name.
function sortByUsername(directMessages: DirectMessageResponse[]): DirectMessageResponse[] {
  return [...directMessages].sort((a, b) => a.username.localeCompare(b.username))
}

// The signed-in user's direct messages: loaded from the backend, then kept
// up to date by DirectMessageCreated (a conversation someone else started
// shows up without reloading the page).
export function useDirectMessages(
  connection: HubConnection | null,
  status: ChatConnectionStatus,
) {
  const [directMessages, setDirectMessages] = useState<DirectMessageResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [requestId, setRequestId] = useState(0)

  useEffect(() => {
    // Same guard as useChannels: ignore responses after unmount or when a
    // newer request started.
    let isCurrent = true

    directMessagesApi
      .getAll()
      .then((data) => {
        if (isCurrent) {
          setDirectMessages(data)
        }
      })
      .catch((err: unknown) => {
        if (isCurrent) {
          setError(getErrorMessage(err))
        }
      })
      .finally(() => {
        if (isCurrent) {
          setIsLoading(false)
        }
      })

    return () => {
      isCurrent = false
    }
  }, [requestId])

  const reload = useCallback(() => {
    setIsLoading(true)
    setError(null)
    setRequestId((id) => id + 1)
  }, [])

  // Also used for the conversation returned by "Nova mensagem": the same
  // conversation may arrive through the event too, and appears once.
  const addDirectMessage = useCallback((directMessage: DirectMessageResponse) => {
    setDirectMessages((current) =>
      current.some((dm) => dm.id === directMessage.id)
        ? current
        : sortByUsername([...current, directMessage]),
    )
  }, [])

  useEffect(() => {
    if (!connection) {
      return
    }

    return onDirectMessageCreated(connection, addDirectMessage)
  }, [connection, addDirectMessage])

  // Events sent while the connection was down are lost: after a reconnect,
  // fetch the list again in the background (no loading state; the current
  // list stays on screen until the new one arrives).
  const previousStatus = useRef(status)

  useEffect(() => {
    if (previousStatus.current === 'reconnecting' && status === 'connected') {
      setRequestId((id) => id + 1)
    }

    previousStatus.current = status
  }, [status])

  return { directMessages, isLoading, error, reload, addDirectMessage }
}
