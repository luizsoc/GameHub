// Mirrors GameHub.Application.DTOs.Messages

export interface MessageResponse {
  id: string
  content: string
  userId: string
  username: string
  channelId: string
  createdAt: string
}

export interface SendMessageRequest {
  content: string
  channelId: string
}
