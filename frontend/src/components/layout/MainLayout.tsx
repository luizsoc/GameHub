import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../../auth/useAuth'
import { useChannels } from '../../hooks/useChannels'
import { useChatConnection } from '../../hooks/useChatConnection'
import type { ChannelResponse } from '../../types/channel'
import AddMemberModal from '../channels/AddMemberModal'
import ChannelSidebar from '../channels/ChannelSidebar'
import CreateChannelModal from '../channels/CreateChannelModal'
import ConnectionIndicator from '../messages/ConnectionIndicator'
import MessagePanel from '../messages/MessagePanel'
import { Brand } from '../ui/Brand'
import { IconButton } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { IconHash, IconLock, IconMenu, IconUserPlus } from '../ui/icons'

// Where the sidebar stops being a drawer; same breakpoint as index.css.
const DESKTOP_QUERY = '(min-width: 768px)'

function MainLayout() {
  const { user, logout } = useAuth()
  const { channels, isLoading, error, reload, addChannel } = useChannels()
  // Single SignalR connection for the authenticated session; logout unmounts
  // this layout, which stops it.
  const chat = useChatConnection()
  const [selectedChannelId, setSelectedChannelId] = useState<string | null>(null)
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isAddMemberOpen, setIsAddMemberOpen] = useState(false)
  const [isDrawerOpen, setIsDrawerOpen] = useState(false)
  const menuButtonRef = useRef<HTMLButtonElement>(null)

  // Derived instead of synced in an effect: falls back to the first channel,
  // which gives the initial auto-selection for free.
  const selectedChannel =
    channels.find((channel) => channel.id === selectedChannelId) ??
    channels.at(0) ??
    null

  function handleChannelCreated(channel: ChannelResponse) {
    addChannel(channel)
    setSelectedChannelId(channel.id)
    setIsCreateOpen(false)
  }

  // While the mobile drawer is open: Esc closes it (unless it belongs to the
  // create-channel dialog), and so does growing into the desktop layout.
  // The cleanup runs once the drawer is hidden again and hands focus back to
  // the menu button.
  useEffect(() => {
    if (!isDrawerOpen) {
      return
    }

    const menuButton = menuButtonRef.current
    const desktop = window.matchMedia(DESKTOP_QUERY)

    function handleKeyDown(event: KeyboardEvent) {
      const inDialog = event.target instanceof Element && event.target.closest('dialog')

      if (event.key === 'Escape' && !inDialog) {
        setIsDrawerOpen(false)
      }
    }

    function handleDesktopChange() {
      if (desktop.matches) {
        setIsDrawerOpen(false)
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    desktop.addEventListener('change', handleDesktopChange)

    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      desktop.removeEventListener('change', handleDesktopChange)
      menuButton?.focus()
    }
  }, [isDrawerOpen])

  function closeDrawer() {
    setIsDrawerOpen(false)
  }

  function renderChannelArea() {
    if (selectedChannel) {
      return (
        <>
          <header className="channel-header">
            <h2 className="channel-title" title={selectedChannel.name}>
              {selectedChannel.isPrivate ? (
                <IconLock size={15} className="channel-lock" />
              ) : (
                <span className="channel-hash" aria-hidden="true">
                  #
                </span>
              )}
              {selectedChannel.name}
              {selectedChannel.isPrivate && (
                <span className="visually-hidden"> (canal privado)</span>
              )}
            </h2>

            {/* No placeholder when the channel has no description. */}
            {selectedChannel.description && (
              <p className="channel-description" title={selectedChannel.description}>
                {selectedChannel.description}
              </p>
            )}

            <ConnectionIndicator status={chat.status} />

            {/* Only private channels have members to manage. Hiding it
                elsewhere is UX; the backend decides who may add members. */}
            {selectedChannel.isPrivate && (
              <IconButton
                label="Adicionar membro"
                icon={<IconUserPlus size={18} />}
                onClick={() => setIsAddMemberOpen(true)}
              />
            )}
          </header>

          <MessagePanel
            channelId={selectedChannel.id}
            channelName={selectedChannel.name}
            chat={chat}
          />
        </>
      )
    }

    if (!isLoading && !error) {
      return (
        <EmptyState
          icon={<IconHash size={20} />}
          title="Nenhum canal por aqui ainda."
          description="Use “Criar canal” para abrir o primeiro e começar a conversar."
        />
      )
    }

    return null
  }

  return (
    <div className={isDrawerOpen ? 'app-shell is-drawer-open' : 'app-shell'}>
      {/* Mobile only: the sidebar becomes a drawer opened from here. Inert
          (like the conversation) while the drawer is open. */}
      <header className="mobile-topbar" inert={isDrawerOpen}>
        <IconButton
          ref={menuButtonRef}
          label="Abrir menu"
          icon={<IconMenu size={20} />}
          className="mobile-menu-button"
          aria-expanded={isDrawerOpen}
          aria-controls="app-sidebar"
          onClick={() => setIsDrawerOpen(true)}
        />
        <h1 className="mobile-topbar-brand">
          <Brand />
        </h1>
      </header>

      <ChannelSidebar
        channels={channels}
        isLoading={isLoading}
        error={error}
        selectedChannelId={selectedChannel?.id ?? null}
        onSelect={(channelId) => {
          setSelectedChannelId(channelId)
          closeDrawer()
        }}
        onRetry={reload}
        onCreateClick={() => setIsCreateOpen(true)}
        username={user?.username ?? ''}
        onLogout={logout}
        isDrawerOpen={isDrawerOpen}
        onCloseDrawer={closeDrawer}
      />

      {/* Pointer shortcut only: Esc and the close button do the same. */}
      <div className="drawer-backdrop" aria-hidden="true" onClick={closeDrawer} />

      <main className="channel-area" inert={isDrawerOpen}>
        {renderChannelArea()}
      </main>

      {isAddMemberOpen && selectedChannel?.isPrivate && (
        <AddMemberModal
          channelId={selectedChannel.id}
          channelName={selectedChannel.name}
          onClose={() => setIsAddMemberOpen(false)}
        />
      )}

      {isCreateOpen && (
        <CreateChannelModal
          onClose={() => setIsCreateOpen(false)}
          onCreated={(channel) => {
            handleChannelCreated(channel)
            closeDrawer()
          }}
        />
      )}
    </div>
  )
}

export default MainLayout
