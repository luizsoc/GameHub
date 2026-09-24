import { useState, type FormEvent, type KeyboardEvent } from 'react'

// Message.Content column limit (GameHubDbContext); the hub does not check it.
const MESSAGE_MAX_LENGTH = 2000
// Show a character counter once the message gets close to the limit.
const COUNTER_THRESHOLD = MESSAGE_MAX_LENGTH - 200

const NOTICE_ID = 'composer-notice'
const ERROR_ID = 'composer-error'
const COUNTER_ID = 'composer-counter'

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
  const showCounter = content.length >= COUNTER_THRESHOLD

  // Ties the notice/error/counter to the field for screen readers, so the
  // reason the send button is disabled is announced with it.
  const describedBy =
    [notice && NOTICE_ID, error && ERROR_ID, showCounter && COUNTER_ID]
      .filter(Boolean)
      .join(' ') || undefined

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

  // Enter sends, Shift+Enter inserts a line break. An Enter that confirms an
  // IME composition (accents, CJK input) must not send the message.
  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) {
      event.preventDefault()
      event.currentTarget.form?.requestSubmit()
    }
  }

  return (
    <div className="composer">
      {notice && (
        <p id={NOTICE_ID} className="composer-notice">
          {notice}
        </p>
      )}

      {error && (
        <p id={ERROR_ID} className="composer-error" role="alert">
          {error}
        </p>
      )}

      <form className="composer-form" onSubmit={handleSubmit} aria-busy={isSending}>
        <textarea
          className="composer-input"
          rows={1}
          value={content}
          maxLength={MESSAGE_MAX_LENGTH}
          placeholder={`Mensagem para #${channelName}`}
          aria-label={`Mensagem para #${channelName}`}
          aria-describedby={describedBy}
          autoComplete="off"
          readOnly={isSending}
          onKeyDown={handleKeyDown}
          onChange={(event) => {
            setContent(event.target.value)
            setError(null)
          }}
        />
        <button type="submit" disabled={!canSend}>
          {isSending ? 'Enviando…' : 'Enviar'}
        </button>
      </form>

      {showCounter && (
        <p id={COUNTER_ID} className="composer-counter">
          {content.length}/{MESSAGE_MAX_LENGTH} caracteres
        </p>
      )}
    </div>
  )
}

export default MessageComposer
