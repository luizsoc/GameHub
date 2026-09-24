import { useCallback, useEffect, useState } from 'react'
import { getErrorMessage } from '../api/errors'
import * as messagesApi from '../api/messages'
import type { MessageResponse } from '../types/message'

// Every result is tagged with the channel it belongs to. The hook only exposes
// a result for the *current* channel, so right after a channel switch the old
// history disappears on the very next render and the state reads as loading.
type MessagesResult =
  | { channelId: string; status: 'success'; messages: MessageResponse[] }
  | { channelId: string; status: 'error'; error: string }

export function useMessages(channelId: string | null) {
  const [result, setResult] = useState<MessagesResult | null>(null)
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

  const current = channelId !== null && result?.channelId === channelId ? result : null

  return {
    messages: current?.status === 'success' ? current.messages : [],
    isLoading: channelId !== null && current === null,
    error: current?.status === 'error' ? current.error : null,
    reload,
  }
}
