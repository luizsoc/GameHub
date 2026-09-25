import { useState } from 'react'
import { useAuth } from '../../auth/useAuth'
import { useChannels } from '../../hooks/useChannels'
import { useChatConnection } from '../../hooks/useChatConnection'
import type { ChannelResponse } from '../../types/channel'
import ChannelSidebar from '../channels/ChannelSidebar'
import CreateChannelModal from '../channels/CreateChannelModal'
import ConnectionIndicator from '../messages/ConnectionIndicator'
import MessagePanel from '../messages/MessagePanel'
import { EmptyState } from '../ui/EmptyState'
import { IconHash } from '../ui/icons'

function MainLayout() {
  const { user, logout } = useAuth()
  const { channels, isLoading, error, reload, addChannel } = useChannels()
  // Single SignalR connection for the authenticated session; logout unmounts
  // this layout, which stops it.
  const chat = useChatConnection()
  const [selectedChannelId, setSelectedChannelId] = useState<string | null>(null)
  const [isCreateOpen, setIsCreateOpen] = useState(false)

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

  function renderChannelArea() {
    if (selectedChannel) {
      return (
        <>
          <header className="channel-header">
            <h2 className="channel-title" title={selectedChannel.name}>
              <span className="channel-hash" aria-hidden="true">
                #
              </span>
              {selectedChannel.name}
            </h2>

            {/* No placeholder when the channel has no description. */}
            {selectedChannel.description && (
              <p className="channel-description" title={selectedChannel.description}>
                {selectedChannel.description}
              </p>
            )}

            <ConnectionIndicator status={chat.status} />
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
    <div className="app-shell">
      <ChannelSidebar
        channels={channels}
        isLoading={isLoading}
        error={error}
        selectedChannelId={selectedChannel?.id ?? null}
        onSelect={setSelectedChannelId}
        onRetry={reload}
        onCreateClick={() => setIsCreateOpen(true)}
        username={user?.username ?? ''}
        onLogout={logout}
      />

      <main className="channel-area">{renderChannelArea()}</main>

      {isCreateOpen && (
        <CreateChannelModal
          onClose={() => setIsCreateOpen(false)}
          onCreated={handleChannelCreated}
        />
      )}
    </div>
  )
}

export default MainLayout
