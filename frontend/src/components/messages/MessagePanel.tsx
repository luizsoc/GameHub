import { useMessages } from '../../hooks/useMessages'
import MessageList from './MessageList'

interface MessagePanelProps {
  channelId: string
}

function MessagePanel({ channelId }: MessagePanelProps) {
  const { messages, isLoading, error, reload } = useMessages(channelId)

  if (isLoading) {
    return (
      <p className="channel-status" role="status">
        Carregando mensagens…
      </p>
    )
  }

  if (error) {
    return (
      <div className="channel-status" role="alert">
        <p className="channel-status-error">{error}</p>
        <button type="button" className="button-secondary" onClick={reload}>
          Tentar novamente
        </button>
      </div>
    )
  }

  if (messages.length === 0) {
    return <p className="channel-status">Nenhuma mensagem ainda.</p>
  }

  return <MessageList messages={messages} />
}

export default MessagePanel
