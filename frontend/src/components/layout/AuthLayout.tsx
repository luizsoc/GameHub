import type { ReactNode } from 'react'
import { Brand } from '../ui/Brand'

interface AuthLayoutProps {
  title: string
  subtitle: string
  // The page's form (state, validation and submit stay in the page).
  children: ReactNode
  // Link to the other auth page.
  footer: ReactNode
}

// Shared frame for login and registration: brand, heading, a card with the
// form and the link to switch pages. Purely presentational.
function AuthLayout({ title, subtitle, children, footer }: AuthLayoutProps) {
  return (
    <main className="auth-page">
      <div className="auth-container">
        <header className="auth-header">
          <Brand />
          <h1 className="auth-title">{title}</h1>
          <p className="auth-subtitle">{subtitle}</p>
        </header>

        <div className="auth-card">{children}</div>

        <p className="auth-switch">{footer}</p>
      </div>
    </main>
  )
}

export default AuthLayout
