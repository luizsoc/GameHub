import type {
  AddChannelMemberRequest,
  ChannelMemberResponse,
  ChannelResponse,
  CreateChannelRequest,
} from '../types/channel'
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

// Private channels only; the backend checks that the caller is a member.
export async function addMember(
  channelId: string,
  request: AddChannelMemberRequest,
): Promise<ChannelMemberResponse> {
  const { data } = await api.post<ChannelMemberResponse>(
    `/channels/${encodeURIComponent(channelId)}/members`,
    request,
  )
  return data
}
