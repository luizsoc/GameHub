import { useState, type FormEvent } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../auth/useAuth'
import { validateEmail, validatePassword } from '../auth/validation'
import FormField from '../components/FormField'

interface LoginErrors {
  email?: string
  password?: string
}

function LoginPage() {
  const { isAuthenticated, login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fieldErrors, setFieldErrors] = useState<LoginErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const errors: LoginErrors = {
      email: validateEmail(email),
      password: validatePassword(password),
    }

    setFieldErrors(errors)
    setFormError(null)

    if (errors.email || errors.password) {
      return
    }

    setIsSubmitting(true)

    try {
      // On success the auth state changes and <Navigate> above takes over.
      await login(email.trim(), password)
    } catch (error) {
      setFormError(getErrorMessage(error))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="auth-page">
      <h1>Entrar no GameHub</h1>

      <form onSubmit={handleSubmit} noValidate aria-busy={isSubmitting}>
        {formError && (
          <p className="form-error" role="alert">
            {formError}
          </p>
        )}

        <FormField
          id="email"
          label="E-mail"
          type="email"
          value={email}
          onChange={setEmail}
          autoComplete="email"
          error={fieldErrors.email}
        />

        <FormField
          id="password"
          label="Senha"
          type="password"
          value={password}
          onChange={setPassword}
          autoComplete="current-password"
          error={fieldErrors.password}
        />

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Entrando…' : 'Entrar'}
        </button>
      </form>

      <p>
        Não tem conta? <Link to="/register">Cadastre-se</Link>
      </p>
    </main>
  )
}

export default LoginPage
