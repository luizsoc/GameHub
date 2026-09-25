import type { ComponentPropsWithRef, ReactNode } from 'react'
import { Spinner } from './icons'

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger'
export type ButtonSize = 'sm' | 'md'

// With ref: React 19 passes it through ...rest to the <button>.
export interface ButtonProps extends ComponentPropsWithRef<'button'> {
  variant?: ButtonVariant
  // sm = 32px, md = 40px
  size?: ButtonSize
  // Shows a spinner and disables the button; the label is kept as passed.
  isLoading?: boolean
  // Leading icon (replaced by the spinner while loading).
  icon?: ReactNode
}

export function Button({
  variant = 'primary',
  size = 'md',
  isLoading = false,
  icon,
  type = 'button',
  disabled,
  className,
  children,
  ...rest
}: ButtonProps) {
  const classes = ['btn', `btn-${variant}`, `btn-${size}`, className]
    .filter(Boolean)
    .join(' ')

  return (
    <button
      type={type}
      className={classes}
      disabled={disabled || isLoading}
      data-loading={isLoading || undefined}
      {...rest}
    >
      {isLoading ? <Spinner /> : icon}
      {children}
    </button>
  )
}

interface IconButtonProps extends Omit<ButtonProps, 'children' | 'icon'> {
  // Accessible name, also shown as a tooltip.
  label: string
  icon: ReactNode
}

export function IconButton({
  label,
  icon,
  variant = 'ghost',
  className,
  ...rest
}: IconButtonProps) {
  return (
    <Button
      {...rest}
      variant={variant}
      icon={icon}
      aria-label={label}
      title={label}
      className={['btn-icon', className].filter(Boolean).join(' ')}
    />
  )
}
