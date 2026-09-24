import type { ChannelResponse } from '../../types/channel'

interface ChannelListProps {
  channels: ChannelResponse[]
  selectedChannelId: string | null
  onSelect: (channelId: string) => void
}

function ChannelList({ channels, selectedChannelId, onSelect }: ChannelListProps) {
  return (
    <nav aria-label="Lista de canais">
      <ul className="channel-list">
        {channels.map((channel) => (
          <li key={channel.id}>
            <button
              type="button"
              className="channel-item"
              aria-current={channel.id === selectedChannelId ? 'true' : undefined}
              title={channel.name}
              onClick={() => onSelect(channel.id)}
            >
              <span className="channel-hash" aria-hidden="true">
                #
              </span>
              <span className="channel-name">{channel.name}</span>
            </button>
          </li>
        ))}
      </ul>
    </nav>
  )
}

export default ChannelList
