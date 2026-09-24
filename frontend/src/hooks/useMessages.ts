import { useCallback, useEffect, useMemo, useState } from 'react'
import { getErrorMessage } from '../api/errors'
import * as messagesApi from '../api/messages'
import type { MessageResponse } from '../types/message'

const NO_MESSAGES: MessageResponse[] = []

// Every result is tagged with the channel it belongs to. The hook only exposes
// a result for the *current* channel, so right after a channel switch the old
// history disappears on the very next render and the state reads as loading.
type MessagesResult =
  | { channelId: string; status: 'success'; messages: MessageResponse[] }
  | { channelId: string; status: 'error'; error: string }

// Realtime messages are kept apart from the HTTP history, also tagged with
// their channel. That way a message that arrives while the history is still
// loading is not lost: it is merged in once the history arrives.
interface LiveMessages {
  channelId: string
  messages: MessageResponse[]
}

// History first (ordered by the API), then realtime messages in arrival order.
// message.id is the only identity: a message present in both appears once.
function mergeById(
  history: MessageResponse[],
  live: MessageResponse[],
): MessageResponse[] {
  if (live.length === 0) {
    return history
  }

  const historyIds = new Set(history.map((message) => message.id))
  const newMessages = live.filter((message) => !historyIds.has(message.id))

  return newMessages.length === 0 ? history : [...history, ...newMessages]
}

export function useMessages(channelId: string | null) {
  const [result, setResult] = useState<MessagesResult | null>(null)
  const [live, setLive] = useState<LiveMessages | null>(null)
  const [requestId, setRequestId] = useState(0)

  useEffect(() => {
    if (!channelId) {
      return
    }

    // Aborting on cleanup cancels the request of a channel the user left
    // (and StrictMode's first run in dev). The aborted checks make sure a
    // late response can never overwrite the current channel's state.
    const controller = new AbortController()

    messagesApi
      .getByChannel(channelId, controller.signal)
      .then((messages) => {
        if (!controller.signal.aborted) {
          setResult({ channelId, status: 'success', messages })
        }
      })
      .catch((err: unknown) => {
        if (!controller.signal.aborted) {
          setResult({ channelId, status: 'error', error: getErrorMessage(err) })
        }
      })

    return () => controller.abort()
  }, [channelId, requestId])

  const reload = useCallback(() => {
    setResult(null)
    setRequestId((id) => id + 1)
  }, [])

  // Functional update: several messages arriving in a burst are all kept,
  // and a repeated id is ignored.
  const addMessage = useCallback((message: MessageResponse) => {
    setLive((current) => {
      const existing =
        current?.channelId === message.channelId ? current.messages : NO_MESSAGES

      if (existing.some((m) => m.id === message.id)) {
        return current
      }

      return { channelId: message.channelId, messages: [...existing, message] }
    })
  }, [])

  const current = channelId !== null && result?.channelId === channelId ? result : null
  const history = current?.status === 'success' ? current.messages : NO_MESSAGES
  const liveMessages =
    channelId !== null && live?.channelId === channelId ? live.messages : NO_MESSAGES

  const messages = useMemo(
    () => mergeById(history, liveMessages),
    [history, liveMessages],
  )

  return {
    messages,
    isLoading: channelId !== null && current === null,
    error: current?.status === 'error' ? current.error : null,
    reload,
    addMessage,
  }
}
