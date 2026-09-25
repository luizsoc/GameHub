import type { ReactNode } from 'react'
import {
  IconAlertCircle,
  IconAlertTriangle,
  IconCheckCircle,
  IconInfo,
} from './icons'

export type AlertVariant = 'error' | 'warning' | 'info' | 'success'

const ICONS: Record<AlertVariant, ReactNode> = {
  error: <IconAlertCircle className="alert-icon" />,
  warning: <IconAlertTriangle className="alert-icon" />,
  info: <IconInfo className="alert-icon" />,
  success: <IconCheckCircle className="alert-icon" />,
}

interface AlertProps {
  variant: AlertVariant
  children: ReactNode
  // Callers keep the role they already had: "alert" for errors that must be
  // announced, "status" for polite notices, none for text that is already
  // announced elsewhere (e.g. through aria-describedby).
  role?: 'alert' | 'status'
  id?: string
  // Optional control shown under the message, e.g. a retry button.
  action?: ReactNode
}

export function Alert({ variant, children, role, id, action }: AlertProps) {
  return (
    <div id={id} role={role} className={`alert alert-${variant}`}>
      {ICONS[variant]}
      <div className="alert-body">
        <div>{children}</div>
        {action && <div className="alert-action">{action}</div>}
      </div>
    </div>
  )
}
