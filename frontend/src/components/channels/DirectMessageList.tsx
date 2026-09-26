import type { DirectMessageResponse } from '../../types/channel'
import { Avatar } from '../ui/Avatar'

interface DirectMessageListProps {
  directMessages: DirectMessageResponse[]
  selectedId: string | null
  onSelect: (directMessage: DirectMessageResponse) => void
}

// Same rows as ChannelList, with the other person's avatar instead of "#".
function DirectMessageList({ directMessages, selectedId, onSelect }: DirectMessageListProps) {
  return (
    <nav aria-label="Lista de mensagens diretas">
      <ul className="channel-list">
        {directMessages.map((directMessage) => (
          <li key={directMessage.id}>
            <button
              type="button"
              className="channel-item"
              aria-current={directMessage.id === selectedId ? 'true' : undefined}
              title={directMessage.username}
              onClick={() => onSelect(directMessage)}
            >
              <Avatar name={directMessage.username} size={20} />
              <span className="channel-name">{directMessage.username}</span>
            </button>
          </li>
        ))}
      </ul>
    </nav>
  )
}

export default DirectMessageList
