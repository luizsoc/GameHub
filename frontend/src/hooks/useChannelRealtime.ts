import { HubConnectionState, type HubConnection } from '@microsoft/signalr'
import { useEffect } from 'react'
import {
  joinChannel,
  leaveChannel,
  onReceiveMessage,
} from '../services/chatConnection'
import type { MessageResponse } from '../types/message'

// Keeps the connection subscribed to exactly one channel: the current one.
export function useChannelRealtime(
  connection: HubConnection | null,
  isConnected: boolean,
  channelId: string,
  onMessage: (message: MessageResponse) => void,
) {
  // One handler at a time, replaced on every channel switch. Messages for the
  // previous channel can still be in flight right after a switch (before the
  // server processes LeaveChannel), so they are filtered out here.
  useEffect(() => {
    if (!connection) {
      return
    }

    return onReceiveMessage(connection, (message) => {
      if (message.channelId === channelId) {
        onMessage(message)
      }
    })
  }, [connection, channelId, onMessage])

  // Group membership. Invocations on one connection are processed in order,
  // so a switch sends LeaveChannel(previous) and then JoinChannel(current).
  //
  // isConnected is a dependency on purpose: an automatic reconnect creates a
  // new connection id without any groups, and this effect then re-runs with
  // the channel selected *at that moment* (never a stale one from before the
  // drop).
  useEffect(() => {
    if (!connection || !isConnected) {
      return
    }

    joinChannel(connection, channelId).catch(() => {
      // Only fails if the connection drops mid-call; the reconnect cycle
      // re-runs this effect and joins again.
    })

    return () => {
      // After a drop the old connection id has no groups left to leave.
      if (connection.state === HubConnectionState.Connected) {
        leaveChannel(connection, channelId).catch(() => {
          // Leaving is best effort; the handler above ignores other channels.
        })
      }
    }
  }, [connection, isConnected, channelId])
}
