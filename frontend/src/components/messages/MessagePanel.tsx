import { useChannelRealtime } from '../../hooks/useChannelRealtime'
import type { ChatConnectionState } from '../../hooks/useChatConnection'
import { useMessages } from '../../hooks/useMessages'
import { sendMessage } from '../../services/chatConnection'
import MessageComposer from './MessageComposer'
import MessageList from './MessageList'

function getConnectionNotice({ status, error }: ChatConnectionState): string | null {
  switch (status) {
    case 'connected':
      return null
    case 'connecting':
      return 'Conectando ao chat…'
    case 'reconnecting':
      return 'Conexão perdida. Tentando reconectar…'
    case 'disconnected':
      return `${error ?? 'Desconectado do chat.'} Recarregue a página para tentar novamente.`
  }
}

interface MessagePanelProps {
  channelId: string
  channelName: string
  chat: ChatConnectionState
}

// History comes from the REST endpoint (useMessages); SignalR only adds what
// happens after that and sends new messages.
function MessagePanel({ channelId, channelName, chat }: MessagePanelProps) {
  const { messages, isLoading, error, reload, addMessage } = useMessages(channelId)
  const { connection, status } = chat
  const isConnected = status === 'connected'

  useChannelRealtime(connection, isConnected, channelId, addMessage)

  async function handleSend(content: string) {
    if (!connection) {
      throw new Error('Chat connection is not available.')
    }

    await sendMessage(connection, { content, channelId })
  }

  function renderHistory() {
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

  return (
    <>
      <div className="channel-body">{renderHistory()}</div>

      {/* Keyed by channel: a draft or send error never carries over. */}
      <MessageComposer
        key={channelId}
        channelName={channelName}
        isConnected={isConnected}
        notice={getConnectionNotice(chat)}
        onSend={handleSend}
      />
    </>
  )
}

export default MessagePanel
