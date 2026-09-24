import type { ChatConnectionStatus } from '../../hooks/useChatConnection'

const LABELS: Record<ChatConnectionStatus, string> = {
  connecting: 'Conectando…',
  connected: 'Conectado',
  reconnecting: 'Reconectando…',
  disconnected: 'Desconectado',
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
