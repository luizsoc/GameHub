import { useState, type FormEvent, type KeyboardEvent } from 'react'
import { Alert, type AlertVariant } from '../ui/Alert'
import { Button } from '../ui/Button'
import { IconSend } from '../ui/icons'

export interface ComposerNotice {
  variant: AlertVariant
  message: string
}

// Message.Content column limit (GameHubDbContext); the hub does not check it.
const MESSAGE_MAX_LENGTH = 2000
// Show a character counter once the message gets close to the limit.
const COUNTER_THRESHOLD = MESSAGE_MAX_LENGTH - 200

const NOTICE_ID = 'composer-notice'
const ERROR_ID = 'composer-error'
const COUNTER_ID = 'composer-counter'
const HINT_ID = 'composer-hint'

interface MessageComposerProps {
  // What the message goes to, as shown: "#geral" or "@ana".
  conversationLabel: string
  isConnected: boolean
  notice: ComposerNotice | null
  onSend: (content: string) => Promise<void>
}

function MessageComposer({
  conversationLabel,
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
  const isAtLimit = content.length >= MESSAGE_MAX_LENGTH

  // Ties the notice/error/counter to the field for screen readers, so the
  // reason the send button is disabled is announced with it. The keyboard
  // hint comes last.
  const describedBy = [
    notice && NOTICE_ID,
    error && ERROR_ID,
    showCounter && COUNTER_ID,
    HINT_ID,
  ]
    .filter(Boolean)
    .join(' ')

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
      {/* No role: announced through the textarea's aria-describedby, and the
          connection indicator already reports the status change. */}
      {notice && (
        <Alert id={NOTICE_ID} variant={notice.variant}>
          {notice.message}
        </Alert>
      )}

      {error && (
        <Alert id={ERROR_ID} variant="error" role="alert">
          {error}
        </Alert>
      )}

      {/* The form is the raised box: field and send button share one frame. */}
      <form className="composer-form" onSubmit={handleSubmit} aria-busy={isSending}>
        <textarea
          className="composer-input"
          rows={1}
          value={content}
          maxLength={MESSAGE_MAX_LENGTH}
          placeholder={`Mensagem para ${conversationLabel}`}
          aria-label={`Mensagem para ${conversationLabel}`}
          aria-describedby={describedBy}
          autoComplete="off"
          readOnly={isSending}
          onKeyDown={handleKeyDown}
          onChange={(event) => {
            setContent(event.target.value)
            setError(null)
          }}
        />
        <Button
          type="submit"
          size="sm"
          icon={<IconSend />}
          disabled={!canSend}
          isLoading={isSending}
        >
          {isSending ? 'Enviando…' : 'Enviar'}
        </Button>
      </form>

      <div className="composer-footer">
        <p id={HINT_ID} className="composer-hint">
          <kbd>Enter</kbd> para enviar <span aria-hidden="true">•</span>{' '}
          <kbd>Shift</kbd>+<kbd>Enter</kbd> para nova linha
        </p>

        {showCounter && (
          <p
            id={COUNTER_ID}
            className={isAtLimit ? 'composer-counter composer-counter-limit' : 'composer-counter'}
          >
            {content.length}/{MESSAGE_MAX_LENGTH} caracteres
          </p>
        )}
      </div>
    </div>
  )
}

export default MessageComposer
