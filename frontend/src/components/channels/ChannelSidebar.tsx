import type { ChannelResponse } from '../../types/channel'
import ChannelList from './ChannelList'

interface ChannelSidebarProps {
  channels: ChannelResponse[]
  isLoading: boolean
  error: string | null
  selectedChannelId: string | null
  onSelect: (channelId: string) => void
  onRetry: () => void
  onCreateClick: () => void
}

function ChannelSidebar({
  channels,
  isLoading,
  error,
  selectedChannelId,
  onSelect,
  onRetry,
  onCreateClick,
}: ChannelSidebarProps) {
  function renderContent() {
    if (isLoading) {
      return (
        <p className="sidebar-message" role="status">
          Carregando canais…
        </p>
      )
    }

    if (error) {
      return (
        <div className="sidebar-message" role="alert">
          <p className="sidebar-error">{error}</p>
          <button type="button" className="button-secondary" onClick={onRetry}>
            Tentar novamente
          </button>
        </div>
      )
    }

    if (channels.length === 0) {
      return <p className="sidebar-message">Nenhum canal ainda.</p>
    }

    return (
      <ChannelList
        channels={channels}
        selectedChannelId={selectedChannelId}
        onSelect={onSelect}
      />
    )
  }

  return (
    <aside className="sidebar" aria-labelledby="channels-heading">
      <div className="sidebar-header">
        <h2 id="channels-heading">Canais</h2>
      </div>

      <div className="sidebar-body">{renderContent()}</div>

      <div className="sidebar-footer">
        <button type="button" onClick={onCreateClick}>
          + Criar canal
        </button>
      </div>
    </aside>
  )
}

export default ChannelSidebar
