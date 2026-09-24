import { useState, type FormEvent } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../auth/useAuth'
import {
  validateEmail,
  validatePassword,
  validateUsername,
} from '../auth/validation'
import FormField from '../components/FormField'

interface RegisterErrors {
  username?: string
  email?: string
  password?: string
}

function RegisterPage() {
  const { isAuthenticated, register } = useAuth()
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fieldErrors, setFieldErrors] = useState<RegisterErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const errors: RegisterErrors = {
      username: validateUsername(username),
      email: validateEmail(email),
      password: validatePassword(password),
    }

    setFieldErrors(errors)
    setFormError(null)

    if (errors.username || errors.email || errors.password) {
      return
    }

    setIsSubmitting(true)

    try {
      // Registers and then logs in automatically.
      await register(username.trim(), email.trim(), password)
    } catch (error) {
      setFormError(getErrorMessage(error))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="auth-page">
      <h1>Criar conta</h1>

      <form onSubmit={handleSubmit} noValidate aria-busy={isSubmitting}>
        {formError && (
          <p className="form-error" role="alert">
            {formError}
          </p>
        )}

        <FormField
          id="username"
          label="Nome de usuário"
          type="text"
          value={username}
          onChange={setUsername}
          autoComplete="username"
          error={fieldErrors.username}
        />

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
          autoComplete="new-password"
          error={fieldErrors.password}
        />

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Cadastrando…' : 'Cadastrar'}
        </button>
      </form>

      <p>
        Já tem conta? <Link to="/login">Entrar</Link>
      </p>
    </main>
  )
}

export default RegisterPage
