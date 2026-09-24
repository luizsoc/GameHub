import { useCallback, useEffect, useState } from 'react'
import * as channelsApi from '../api/channels'
import { getErrorMessage } from '../api/errors'
import type { ChannelResponse } from '../types/channel'

// The backend already returns channels ordered by name; keep that order
// when a newly created channel is added locally.
function sortByName(channels: ChannelResponse[]): ChannelResponse[] {
  return [...channels].sort((a, b) => a.name.localeCompare(b.name))
}

export function useChannels() {
  const [channels, setChannels] = useState<ChannelResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [requestId, setRequestId] = useState(0)

  useEffect(() => {
    // Ignores the response if the component unmounted or a newer
    // request started (also covers StrictMode's double effect in dev).
    let isCurrent = true

    channelsApi
      .getAll()
      .then((data) => {
        if (isCurrent) {
          setChannels(data)
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

  const addChannel = useCallback((channel: ChannelResponse) => {
    setChannels((current) =>
      sortByName([...current.filter((c) => c.id !== channel.id), channel]),
    )
  }, [])

  return { channels, isLoading, error, reload, addChannel }
}
