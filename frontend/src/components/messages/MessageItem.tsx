import type { MessageResponse } from '../../types/message'
import { Avatar } from '../ui/Avatar'

const timeFormatter = new Intl.DateTimeFormat('pt-BR', {
  hour: '2-digit',
  minute: '2-digit',
})

// Tooltip text, e.g. "quinta-feira, 24 de setembro de 2026 às 10:42".
const fullDateFormatter = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'full',
  timeStyle: 'short',
})

interface MessageItemProps {
  message: MessageResponse
  // Parsed createdAt; null when the backend value cannot be read.
  date: Date | null
  // First message of a visual group: shows avatar, author and time.
  isGroupStart: boolean
  isOwn: boolean
}

function MessageItem({ message, date, isGroupStart, isOwn }: MessageItemProps) {
  const time = date ? timeFormatter.format(date) : message.createdAt
  const fullDate = date ? fullDateFormatter.format(date) : message.createdAt

  if (isGroupStart) {
    return (
      <li className="message message-group-start">
        <Avatar name={message.username} size={40} />
        <div className="message-main">
          <div className="message-header">
            <span
              className={isOwn ? 'message-author message-author-own' : 'message-author'}
              title={message.username}
            >
              {message.username}
            </span>
            <time className="message-time" dateTime={message.createdAt} title={fullDate}>
              {time}
            </time>
          </div>
          <p className="message-content">{message.content}</p>
        </div>
      </li>
    )
  }

  // Follow-up in a group: the time only shows on hover, in the avatar column.
  // Screen readers get author and time from the hidden text instead, so each
  // message still reads on its own (also when the log announces it).
  return (
    <li className="message">
      <time
        className="message-hover-time"
        dateTime={message.createdAt}
        title={fullDate}
        aria-hidden="true"
      >
        {time}
      </time>
      <div className="message-main">
        <span className="visually-hidden">
          {message.username}, {time}
        </span>
        <p className="message-content">{message.content}</p>
      </div>
    </li>
  )
}

export default MessageItem
