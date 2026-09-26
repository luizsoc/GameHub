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

// A direct message (a private channel with two members) as seen by the
// signed-in user: id is the channel, userId/username the other participant.
export interface DirectMessageResponse {
  id: string
  userId: string
  username: string
  createdAt: string
}

export interface OpenDirectMessageRequest {
  userId: string
}

export interface ChannelMemberResponse {
  userId: string
  username: string
  channelId: string
  joinedAt: string
}
