import { useLayoutEffect, useMemo, useRef } from 'react'
import { useAuth } from '../../auth/useAuth'
import type { MessageResponse } from '../../types/message'
import MessageItem from './MessageItem'

// The backend sends 7 fractional digits (e.g. "...17.0068076Z"); only 3 are
// guaranteed by the ECMAScript date format, so trim before parsing.
function parseMessageDate(isoDate: string): Date | null {
  const date = new Date(isoDate.replace(/(\.\d{3})\d+/, '$1'))

  return Number.isNaN(date.getTime()) ? null : date
}

const dayFormatter = new Intl.DateTimeFormat('pt-BR', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
})

const dayWithYearFormatter = new Intl.DateTimeFormat('pt-BR', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
  year: 'numeric',
})

// Calendar day in the viewer's time zone.
function getDayKey(date: Date): string {
  return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`
}

// "Hoje", "Ontem" or e.g. "Quarta-feira, 23 de setembro".
function formatDayLabel(date: Date): string {
  const today = new Date()
  const yesterday = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 1)

  if (getDayKey(date) === getDayKey(today)) {
    return 'Hoje'
  }

  if (getDayKey(date) === getDayKey(yesterday)) {
    return 'Ontem'
  }

  const formatter =
    date.getFullYear() === today.getFullYear() ? dayFormatter : dayWithYearFormatter
  const label = formatter.format(date)

  return label.charAt(0).toUpperCase() + label.slice(1)
}

// A group holds consecutive messages from one author sent within this window
// of the group's first message.
const GROUP_WINDOW_MS = 5 * 60 * 1000

interface MessageRow {
  message: MessageResponse
  date: Date | null
  isGroupStart: boolean
  // Day separator shown before this message, if it starts a new day.
  dayLabel: string | null
}

// Display-only view of the list: one row per message, same order, nothing
// merged or dropped. The messages themselves are not touched.
function buildRows(messages: MessageResponse[]): MessageRow[] {
  const rows: MessageRow[] = []
  let group: { userId: string; startTime: number } | null = null
  let previousDayKey: string | null = null

  for (const message of messages) {
    const date = parseMessageDate(message.createdAt)
    const dayKey = date ? getDayKey(date) : null
    const dayLabel = date && dayKey !== previousDayKey ? formatDayLabel(date) : null
    const elapsed = date && group ? date.getTime() - group.startTime : Number.NaN

    const isGroupStart =
      group === null ||
      dayLabel !== null ||
      message.userId !== group.userId ||
      !(elapsed >= 0 && elapsed <= GROUP_WINDOW_MS)

    if (isGroupStart) {
      group = date ? { userId: message.userId, startTime: date.getTime() } : null
    }

    if (dayKey) {
      previousDayKey = dayKey
    }

    rows.push({ message, date, isGroupStart, dayLabel })
  }

  return rows
}

// How close to the bottom (px) still counts as "following" the conversation.
const STICK_TO_BOTTOM_THRESHOLD_PX = 64

interface MessageListProps {
  messages: MessageResponse[]
}

function MessageList({ messages }: MessageListProps) {
  const { user } = useAuth()
  const rows = useMemo(() => buildRows(messages), [messages])
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
        {rows.flatMap(({ message, date, isGroupStart, dayLabel }) => {
          const item = (
            <MessageItem
              key={message.id}
              message={message}
              date={date}
              isGroupStart={isGroupStart}
              isOwn={message.userId === user?.id}
            />
          )

          // Separator keyed by the message it precedes, so it stays unique.
          return dayLabel
            ? [
                <li key={`day-${message.id}`} className="message-day">
                  <span className="message-day-label">{dayLabel}</span>
                </li>,
                item,
              ]
            : [item]
        })}
      </ol>
    </div>
  )
}

export default MessageList
