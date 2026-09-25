import type { ChatConnectionStatus } from '../../hooks/useChatConnection'

// Realtime status only; the signed-in user is shown in the sidebar.
const LABELS: Record<ChatConnectionStatus, string> = {
  connecting: 'Conectando',
  connected: 'Online',
  reconnecting: 'Reconectando',
  disconnected: 'Offline',
}

interface ConnectionIndicatorProps {
  status: ChatConnectionStatus
}

function ConnectionIndicator({ status }: ConnectionIndicatorProps) {
  return (
    <span className={`connection-indicator connection-${status}`} role="status">
      <span className="connection-dot" aria-hidden="true" />
      {LABELS[status]}
    </span>
  )
}

export default ConnectionIndicator
