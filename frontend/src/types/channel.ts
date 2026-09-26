// Mirrors GameHub.Application.DTOs.Channels

export interface ChannelResponse {
  id: string
  name: string
  description: string | null
  isPrivate: boolean
  createdAt: string
}

export interface CreateChannelRequest {
  name: string
  description?: string | null
  isPrivate?: boolean
}

export interface AddChannelMemberRequest {
  username: string
}

export interface ChannelMemberResponse {
  userId: string
  username: string
  channelId: string
  joinedAt: string
}
