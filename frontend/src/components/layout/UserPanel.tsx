import { Avatar } from '../ui/Avatar'
import { Button } from '../ui/Button'
import { IconLogOut } from '../ui/icons'

interface UserPanelProps {
  username: string
  onLogout: () => void
}

// Session block at the bottom of the sidebar: who is signed in and how to
// leave. The realtime connection status lives in the channel header instead.
function UserPanel({ username, onLogout }: UserPanelProps) {
  return (
    <div className="user-panel">
      <Avatar name={username} />
      <span className="user-panel-name" title={username}>
        {username}
      </span>
      <Button variant="ghost" size="sm" icon={<IconLogOut />} onClick={onLogout}>
        Sair
      </Button>
    </div>
  )
}

export default UserPanel
