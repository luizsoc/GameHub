import type { DirectMessageResponse, OpenDirectMessageRequest } from '../types/channel'
import { api } from './client'

export async function getAll(): Promise<DirectMessageResponse[]> {
  const { data } = await api.get<DirectMessageResponse[]>('/direct-messages')
  return data
}

// Returns the existing conversation with that user, or creates it.
export async function open(
  request: OpenDirectMessageRequest,
): Promise<DirectMessageResponse> {
  const { data } = await api.post<DirectMessageResponse>('/direct-messages', request)
  return data
}
