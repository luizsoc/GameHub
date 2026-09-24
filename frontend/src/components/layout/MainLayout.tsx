import { useState } from 'react'
import { useAuth } from '../../auth/useAuth'
import { useChannels } from '../../hooks/useChannels'
import type { ChannelResponse } from '../../types/channel'
import ChannelSidebar from '../channels/ChannelSidebar'
import CreateChannelModal from '../channels/CreateChannelModal'
import MessagePanel from '../messages/MessagePanel'

function MainLayout() {
  const { user, logout } = useAuth()
  const { channels, isLoading, error, reload, addChannel } = useChannels()
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
            <h2>
              <span className="channel-hash" aria-hidden="true">
                #{' '}
              </span>
              {selectedChannel.name}
            </h2>
            <p className="channel-description">
              {selectedChannel.description || 'Sem descrição.'}
            </p>
          </header>

          <MessagePanel channelId={selectedChannel.id} />
        </>
      )
    }

    if (!isLoading && !error) {
      return (
        <p className="channel-status">
          Nenhum canal ainda. Crie o primeiro canal para começar.
        </p>
      )
    }

    return null
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <h1 className="app-title">GameHub</h1>

        <div className="user-menu">
          <span>
            <span className="user-label">Conectado como </span>
            <strong>{user?.username}</strong>
          </span>
          <button type="button" className="button-secondary" onClick={logout}>
            Sair
          </button>
        </div>
      </header>

      <ChannelSidebar
        channels={channels}
        isLoading={isLoading}
        error={error}
        selectedChannelId={selectedChannel?.id ?? null}
        onSelect={setSelectedChannelId}
        onRetry={reload}
        onCreateClick={() => setIsCreateOpen(true)}
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
