import { useLayoutEffect, useRef } from 'react'
import type { MessageResponse } from '../../types/message'

const dateFormatter = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

// The backend sends 7 fractional digits (e.g. "...17.0068076Z"); only 3 are
// guaranteed by the ECMAScript date format, so trim before parsing.
function formatMessageDate(isoDate: string): string {
  const date = new Date(isoDate.replace(/(\.\d{3})\d+/, '$1'))

  return Number.isNaN(date.getTime()) ? isoDate : dateFormatter.format(date)
}

// How close to the bottom (px) still counts as "following" the conversation.
const STICK_TO_BOTTOM_THRESHOLD_PX = 64

interface MessageListProps {
  messages: MessageResponse[]
}

function MessageList({ messages }: MessageListProps) {
  const scrollRef = useRef<HTMLDivElement>(null)
  // Starts true so the history opens at the most recent message.
  const isAtBottomRef = useRef(true)

  function handleScroll() {
    const container = scrollRef.current

    if (container) {
      const distanceFromBottom =
        container.scrollHeight - container.scrollTop - container.clientHeight

      isAtBottomRef.current = distanceFromBottom <= STICK_TO_BOTTOM_THRESHOLD_PX
    }
  }

  // Follows new messages only while the user is at the bottom; someone
  // reading older messages is never pulled away from them.
  // Layout effect: runs before paint, so there is no visible jump.
  useLayoutEffect(() => {
    const container = scrollRef.current

    if (container && isAtBottomRef.current) {
      container.scrollTop = container.scrollHeight
    }
  }, [messages])

  // role="log" (implicitly aria-live="polite") announces messages that arrive
  // after the history is on screen; the history itself is not read out.
  // tabIndex lets keyboard users focus the region and scroll it.
  return (
    <div
      className="message-scroll"
      ref={scrollRef}
      onScroll={handleScroll}
      role="log"
      aria-label="Mensagens do canal"
      tabIndex={0}
    >
      <ol className="message-list">
        {messages.map((message) => (
          <li key={message.id} className="message">
            <div className="message-meta">
              <span className="message-author">{message.username}</span>
              <time className="message-time" dateTime={message.createdAt}>
                {formatMessageDate(message.createdAt)}
              </time>
            </div>
            <p className="message-content">{message.content}</p>
          </li>
        ))}
      </ol>
    </div>
  )
}

export default MessageList
