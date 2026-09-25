import type { CSSProperties } from 'react'

interface SkeletonProps {
  width?: CSSProperties['width']
  height?: CSSProperties['height']
  className?: string
}

// Placeholder block. Purely visual: the loading container that uses it keeps
// a role="status" with readable text for assistive technologies.
export function Skeleton({ width = '100%', height = 12, className }: SkeletonProps) {
  return (
    <span
      className={['skeleton', className].filter(Boolean).join(' ')}
      style={{ width, height }}
      aria-hidden="true"
    />
  )
}
