import type { ChannelResponse, CreateChannelRequest } from '../types/channel'
import { api } from './client'

export async function getAll(): Promise<ChannelResponse[]> {
  const { data } = await api.get<ChannelResponse[]>('/channels')
  return data
}

export async function getById(id: string): Promise<ChannelResponse> {
  const { data } = await api.get<ChannelResponse>(
    `/channels/${encodeURIComponent(id)}`,
  )
  return data
}

export async function create(
  request: CreateChannelRequest,
): Promise<ChannelResponse> {
  const { data } = await api.post<ChannelResponse>('/channels', request)
  return data
}
