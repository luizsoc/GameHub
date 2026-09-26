import {
  HubConnectionBuilder,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr'
import { getToken } from '../auth/tokenStorage'
import { CHAT_HUB_URL } from '../config'
import type { DirectMessageResponse } from '../types/channel'
import type { MessageResponse, SendMessageRequest } from '../types/message'

// Contract of backend/GameHub.Api/Hubs/ChatHub.cs
const RECEIVE_MESSAGE = 'ReceiveMessage'
const DIRECT_MESSAGE_CREATED = 'DirectMessageCreated'

export function createChatConnection(): HubConnection {
  return (
    new HubConnectionBuilder()
      .withUrl(CHAT_HUB_URL, {
        // Read on every (re)connect, so it always uses the current token from
        // the existing storage. In the browser the client sends it as
        // ?access_token=… on the WebSocket request, which the backend accepts
        // only for /hubs/chat.
        accessTokenFactory: () => {
          const token = getToken()

          if (!token) {
            throw new Error('Not authenticated.')
          }

          return token
        },
      })
      // Default policy: retries after 0, 2, 10 and 30 seconds, then gives up.
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()
  )
}

export function joinChannel(
  connection: HubConnection,
  channelId: string,
): Promise<void> {
  return connection.invoke<void>('JoinChannel', channelId)
}

export function leaveChannel(
  connection: HubConnection,
  channelId: string,
): Promise<void> {
  return connection.invoke<void>('LeaveChannel', channelId)
}

// Resolves once the server has stored and broadcast the message. The message
// itself reaches the UI through ReceiveMessage, like everyone else's.
export function sendMessage(
  connection: HubConnection,
  request: SendMessageRequest,
): Promise<void> {
  return connection.invoke<void>('SendMessage', request)
}

export function onReceiveMessage(
  connection: HubConnection,
  handler: (message: MessageResponse) => void,
): () => void {
  connection.on(RECEIVE_MESSAGE, handler)

  return () => connection.off(RECEIVE_MESSAGE, handler)
}

// Sent by the server to both participants when a direct message is created
// (not to anyone else), each seeing the other participant.
export function onDirectMessageCreated(
  connection: HubConnection,
  handler: (directMessage: DirectMessageResponse) => void,
): () => void {
  connection.on(DIRECT_MESSAGE_CREATED, handler)

  return () => connection.off(DIRECT_MESSAGE_CREATED, handler)
}
