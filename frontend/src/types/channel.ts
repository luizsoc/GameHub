// Mirrors GameHub.Application.DTOs.Channels

export interface ChannelResponse {
  id: string
  name: string
  description: string | null
  createdAt: string
}

export interface CreateChannelRequest {
  name: string
  description?: string | null
}
