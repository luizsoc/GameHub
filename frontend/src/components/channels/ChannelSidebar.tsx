import { useEffect, useRef } from 'react'
import type { ChannelResponse } from '../../types/channel'
import UserPanel from '../layout/UserPanel'
import { Alert } from '../ui/Alert'
import { Brand } from '../ui/Brand'
import { Button, IconButton } from '../ui/Button'
import { IconPlus, IconX } from '../ui/icons'
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
  // Mobile drawer (below 768px); the desktop sidebar ignores both.
  isDrawerOpen: boolean
  onCloseDrawer: () => void
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
  isDrawerOpen,
  onCloseDrawer,
}: ChannelSidebarProps) {
  const closeButtonRef = useRef<HTMLButtonElement>(null)

  // Opening the drawer moves focus into it.
  useEffect(() => {
    if (isDrawerOpen) {
      closeButtonRef.current?.focus()
    }
  }, [isDrawerOpen])

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
    <aside id="app-sidebar" className="sidebar">
      {/* The page's <h1>: the brand (the mobile top bar has its own while
          this sidebar is a closed drawer). */}
      <h1 className="sidebar-brand">
        <Brand />
      </h1>

      {/* Drawer only; hidden on desktop. */}
      <IconButton
        ref={closeButtonRef}
        label="Fechar menu"
        icon={<IconX size={20} />}
        className="drawer-close"
        onClick={onCloseDrawer}
      />

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
