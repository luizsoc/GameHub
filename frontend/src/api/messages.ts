import type { MessageResponse, SendMessageRequest } from '../types/message'
import { api } from './client'

export async function getByChannel(
  channelId: string,
  signal?: AbortSignal,
): Promise<MessageResponse[]> {
  const { data } = await api.get<MessageResponse[]>(
    `/messages/channel/${encodeURIComponent(channelId)}`,
    { signal },
  )
  return data
}

// Note: the REST endpoint persists the message but does not broadcast it
// over SignalR. Realtime sending will go through the ChatHub.
export async function send(
  request: SendMessageRequest,
): Promise<MessageResponse> {
  const { data } = await api.post<MessageResponse>('/messages', request)
  return data
}
