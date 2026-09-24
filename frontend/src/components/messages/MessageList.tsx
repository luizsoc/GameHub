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

interface MessageListProps {
  messages: MessageResponse[]
}

function MessageList({ messages }: MessageListProps) {
  const scrollRef = useRef<HTMLDivElement>(null)

  // Show the most recent messages once the history is rendered.
  // Layout effect: runs before paint, so there is no visible jump.
  useLayoutEffect(() => {
    const container = scrollRef.current

    if (container) {
      container.scrollTop = container.scrollHeight
    }
  }, [messages])

  return (
    <div className="message-scroll" ref={scrollRef}>
      <ol className="message-list" aria-label="Mensagens">
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
