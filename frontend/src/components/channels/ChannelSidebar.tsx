import type { ChannelResponse } from '../../types/channel'
import UserPanel from '../layout/UserPanel'
import { Alert } from '../ui/Alert'
import { Brand } from '../ui/Brand'
import { Button, IconButton } from '../ui/Button'
import { IconPlus } from '../ui/icons'
import { Skeleton } from '../ui/Skeleton'
import ChannelList from './ChannelList'

// Varied widths so the placeholder reads as a list of names.
const SKELETON_WIDTHS = ['72%', '58%', '84%', '64%', '48%']

interface ChannelSidebarProps {
  channels: ChannelResponse[]
  isLoading: boolean
  error: string | null
  selectedChannelId: string | null
  onSelect: (channelId: string) => void
  onRetry: () => void
  onCreateClick: () => void
  username: string
  onLogout: () => void
}

function ChannelSidebar({
  channels,
  isLoading,
  error,
  selectedChannelId,
  onSelect,
  onRetry,
  onCreateClick,
  username,
  onLogout,
}: ChannelSidebarProps) {
  function renderContent() {
    if (isLoading) {
      return (
        <div className="channel-skeleton" role="status">
          <span className="visually-hidden">Carregando canais…</span>
          {SKELETON_WIDTHS.map((width) => (
            <div key={width} className="channel-skeleton-item">
              <Skeleton width={width} height={12} />
            </div>
          ))}
        </div>
      )
    }

    if (error) {
      return (
        <div className="sidebar-message">
          <Alert
            variant="error"
            role="alert"
            action={
              <Button variant="secondary" size="sm" onClick={onRetry}>
                Tentar novamente
              </Button>
            }
          >
            {error}
          </Alert>
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
    <aside className="sidebar">
      {/* The page's only <h1>: the brand. */}
      <h1 className="sidebar-brand">
        <Brand />
      </h1>

      <section className="sidebar-section" aria-labelledby="channels-heading">
        <div className="sidebar-header">
          <h2 id="channels-heading">Canais</h2>
          <IconButton
            label="Criar canal"
            icon={<IconPlus />}
            size="sm"
            onClick={onCreateClick}
          />
        </div>

        <div className="sidebar-body">{renderContent()}</div>
      </section>

      <UserPanel username={username} onLogout={onLogout} />
    </aside>
  )
}

export default ChannelSidebar
