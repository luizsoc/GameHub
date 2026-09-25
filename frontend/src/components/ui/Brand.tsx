import { IconHash } from './icons'

// GameHub wordmark: violet square with the channel "#" plus the name.
// Only phrasing elements (spans), so it can sit inside a heading.
export function Brand() {
  return (
    <span className="brand">
      <span className="brand-mark" aria-hidden="true">
        <IconHash size={18} strokeWidth={2.5} />
      </span>
      <span className="brand-name">GameHub</span>
    </span>
  )
}
