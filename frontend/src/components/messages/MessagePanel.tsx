import { useChannelRealtime } from '../../hooks/useChannelRealtime'
import type { ChatConnectionState } from '../../hooks/useChatConnection'
import { useMessages } from '../../hooks/useMessages'
import { sendMessage } from '../../services/chatConnection'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { IconMessage } from '../ui/icons'
import { Skeleton } from '../ui/Skeleton'
import MessageComposer, { type ComposerNotice } from './MessageComposer'
import MessageList from './MessageList'

// Author/content widths for the loading placeholder rows.
const SKELETON_ROWS = [
  ['96px', '64%'],
  ['120px', '42%'],
  ['80px', '78%'],
  ['104px', '52%'],
]

function getConnectionNotice({ status, error }: ChatConnectionState): ComposerNotice | null {
  switch (status) {
    case 'connected':
      return null
    case 'connecting':
      return { variant: 'info', message: 'Conectando ao chat…' }
    case 'reconnecting':
      return { variant: 'warning', message: 'Conexão perdida. Tentando reconectar…' }
    case 'disconnected':
      return {
        variant: 'error',
        message: `${error ?? 'Desconectado do chat.'} Recarregue a página para tentar novamente.`,
      }
  }
}

interface MessagePanelProps {
  // A channel or a direct message: both are channels in the backend.
  channelId: string
  // As shown in the UI: "#geral" for a channel, "@ana" for a direct message.
  conversationLabel: string
  isDirectMessage?: boolean
  chat: ChatConnectionState
}

// History comes from the REST endpoint (useMessages); SignalR only adds what
// happens after that and sends new messages.
function MessagePanel({
  channelId,
  conversationLabel,
  isDirectMessage = false,
  chat,
}: MessagePanelProps) {
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
        <div className="message-skeleton" role="status">
          <span className="visually-hidden">Carregando mensagens…</span>
          {SKELETON_ROWS.map(([authorWidth, contentWidth]) => (
            <div key={authorWidth} className="message-skeleton-row">
              <Skeleton width={40} height={40} className="skeleton-avatar" />
              <div className="message-skeleton-lines">
                <Skeleton width={authorWidth} height={12} />
                <Skeleton width={contentWidth} height={14} />
              </div>
            </div>
          ))}
        </div>
      )
    }

    if (error) {
      return (
        <div className="channel-status">
          <Alert
            variant="error"
            role="alert"
            action={
              <Button variant="secondary" size="sm" onClick={reload}>
                Tentar novamente
              </Button>
            }
          >
            {error}
          </Alert>
        </div>
      )
    }

    if (messages.length === 0) {
      return (
        <EmptyState
          icon={<IconMessage size={20} />}
          title={
            isDirectMessage
              ? `Ainda não há mensagens com ${conversationLabel}.`
              : `Ainda não há mensagens em ${conversationLabel}.`
          }
          description="Envie a primeira mensagem para começar a conversa."
        />
      )
    }

    return <MessageList messages={messages} />
  }

  return (
    <>
      <div className="channel-body">{renderHistory()}</div>

      {/* Keyed by channel: a draft or send error never carries over. */}
      <MessageComposer
        key={channelId}
        conversationLabel={conversationLabel}
        isConnected={isConnected}
        notice={getConnectionNotice(chat)}
        onSend={handleSend}
      />
    </>
  )
}

export default MessagePanel
