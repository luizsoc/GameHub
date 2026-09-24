import { useState, type FormEvent } from 'react'

// Message.Content column limit (GameHubDbContext); the hub does not check it.
const MESSAGE_MAX_LENGTH = 2000

interface MessageComposerProps {
  channelName: string
  isConnected: boolean
  notice: string | null
  onSend: (content: string) => Promise<void>
}

function MessageComposer({
  channelName,
  isConnected,
  notice,
  onSend,
}: MessageComposerProps) {
  const [content, setContent] = useState('')
  const [isSending, setIsSending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const trimmedContent = content.trim()
  const canSend = isConnected && !isSending && trimmedContent !== ''

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!canSend) {
      return
    }

    setIsSending(true)
    setError(null)

    try {
      // No optimistic message: it appears when ReceiveMessage arrives.
      await onSend(trimmedContent)
      setContent('')
    } catch {
      setError('Não foi possível enviar a mensagem.')
    } finally {
      setIsSending(false)
    }
  }

  return (
    <div className="composer">
      {notice && <p className="composer-notice">{notice}</p>}

      {error && (
        <p className="composer-error" role="alert">
          {error}
        </p>
      )}

      <form className="composer-form" onSubmit={handleSubmit}>
        <input
          className="composer-input"
          type="text"
          value={content}
          maxLength={MESSAGE_MAX_LENGTH}
          placeholder={`Mensagem para #${channelName}`}
          aria-label={`Mensagem para #${channelName}`}
          autoComplete="off"
          readOnly={isSending}
          onChange={(event) => {
            setContent(event.target.value)
            setError(null)
          }}
        />
        <button type="submit" disabled={!canSend}>
          {isSending ? 'Enviando…' : 'Enviar'}
        </button>
      </form>
    </div>
  )
}

export default MessageComposer
