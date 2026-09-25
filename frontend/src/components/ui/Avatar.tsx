// "ui_tester" → "UT", "maria" → "MA". Only for display.
function getInitials(name: string): string {
  const trimmed = name.trim()
  const parts = trimmed.split(/[\s._-]+/).filter(Boolean)
  const letters = parts.length >= 2 ? parts[0][0] + parts[1][0] : trimmed.slice(0, 2)

  return letters.toUpperCase()
}

interface AvatarProps {
  name: string
  size?: number
}

// Initials avatar. Decorative: the name is always shown next to it.
export function Avatar({ name, size = 32 }: AvatarProps) {
  return (
    <span
      className="avatar"
      style={{ width: size, height: size, fontSize: Math.round(size * 0.4) }}
      aria-hidden="true"
    >
      {getInitials(name)}
    </span>
  )
}
