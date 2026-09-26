import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../../auth/useAuth'
import { useChannels } from '../../hooks/useChannels'
import { useChatConnection } from '../../hooks/useChatConnection'
import { useDirectMessages } from '../../hooks/useDirectMessages'
import type { ChannelResponse, DirectMessageResponse } from '../../types/channel'
import AddMemberModal from '../channels/AddMemberModal'
import ChannelSidebar from '../channels/ChannelSidebar'
import CreateChannelModal from '../channels/CreateChannelModal'
import NewDirectMessageModal from '../channels/NewDirectMessageModal'
import ConnectionIndicator from '../messages/ConnectionIndicator'
import MessagePanel from '../messages/MessagePanel'
import { Avatar } from '../ui/Avatar'
import { Brand } from '../ui/Brand'
import { IconButton } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { IconHash, IconLock, IconMenu, IconUserPlus } from '../ui/icons'

// Where the sidebar stops being a drawer; same breakpoint as index.css.
const DESKTOP_QUERY = '(min-width: 768px)'

// What is open in the conversation area. Both are channels in the backend;
// they only come from different lists.
type Selection = { kind: 'channel'; id: string } | { kind: 'directMessage'; id: string }

function MainLayout() {
  const { user, logout } = useAuth()
  const { channels, isLoading, error, reload, addChannel } = useChannels()
  // Single SignalR connection for the authenticated session; logout unmounts
  // this layout, which stops it.
  const chat = useChatConnection()
  const dms = useDirectMessages(chat.connection, chat.status)
  const [selection, setSelection] = useState<Selection | null>(null)
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isNewDirectMessageOpen, setIsNewDirectMessageOpen] = useState(false)
  const [isAddMemberOpen, setIsAddMemberOpen] = useState(false)
  const [isDrawerOpen, setIsDrawerOpen] = useState(false)
  const menuButtonRef = useRef<HTMLButtonElement>(null)

  // Derived instead of synced in an effect. A selected direct message wins;
  // otherwise the selected channel, falling back to the first channel, which
  // gives the initial auto-selection for free.
  const selectedDirectMessage =
    selection?.kind === 'directMessage'
      ? (dms.directMessages.find((dm) => dm.id === selection.id) ?? null)
      : null

  const selectedChannel = selectedDirectMessage
    ? null
    : (channels.find((channel) => selection?.kind === 'channel' && channel.id === selection.id) ??
      channels.at(0) ??
      null)

  function handleChannelCreated(channel: ChannelResponse) {
    addChannel(channel)
    setSelection({ kind: 'channel', id: channel.id })
    setIsCreateOpen(false)
  }

  // The conversation returned by "Nova mensagem" (existing or new) is added
  // right away; its DirectMessageCreated event, if any, is then a no-op.
  function handleDirectMessageOpened(directMessage: DirectMessageResponse) {
    dms.addDirectMessage(directMessage)
    setSelection({ kind: 'directMessage', id: directMessage.id })
    setIsNewDirectMessageOpen(false)
  }

  // While the mobile drawer is open: Esc closes it (unless it belongs to an
  // open dialog), and so does growing into the desktop layout.
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
    if (selectedDirectMessage) {
      return (
        <>
          <header className="channel-header">
            <h2 className="channel-title" title={selectedDirectMessage.username}>
              <Avatar name={selectedDirectMessage.username} size={24} />
              {selectedDirectMessage.username}
              <span className="visually-hidden"> (mensagem direta)</span>
            </h2>

            <ConnectionIndicator status={chat.status} />
          </header>

          {/* The same chat as channels: history, SignalR group and composer
              all use the conversation's channel id. */}
          <MessagePanel
            channelId={selectedDirectMessage.id}
            conversationLabel={`@${selectedDirectMessage.username}`}
            isDirectMessage
            chat={chat}
          />
        </>
      )
    }

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
            conversationLabel={`#${selectedChannel.name}`}
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
          setSelection({ kind: 'channel', id: channelId })
          closeDrawer()
        }}
        onRetry={reload}
        onCreateClick={() => setIsCreateOpen(true)}
        directMessages={dms.directMessages}
        isDirectMessagesLoading={dms.isLoading}
        directMessagesError={dms.error}
        selectedDirectMessageId={selectedDirectMessage?.id ?? null}
        onSelectDirectMessage={(directMessage) => {
          setSelection({ kind: 'directMessage', id: directMessage.id })
          closeDrawer()
        }}
        onRetryDirectMessages={dms.reload}
        onNewDirectMessageClick={() => setIsNewDirectMessageOpen(true)}
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

      {isNewDirectMessageOpen && (
        <NewDirectMessageModal
          onClose={() => setIsNewDirectMessageOpen(false)}
          onOpened={(directMessage) => {
            handleDirectMessageOpened(directMessage)
            closeDrawer()
          }}
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
