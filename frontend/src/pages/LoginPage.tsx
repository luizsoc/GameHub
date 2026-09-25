import { useState, type FormEvent } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../auth/useAuth'
import { validateEmail, validatePassword } from '../auth/validation'
import FormField from '../components/FormField'
import AuthLayout from '../components/layout/AuthLayout'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'

interface LoginErrors {
  email?: string
  password?: string
}

function LoginPage() {
  const { isAuthenticated, sessionExpired, login } = useAuth()
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
      // AuthController answers 401 only for invalid credentials.
      setFormError(
        getErrorMessage(error, { 401: 'E-mail ou senha inválidos.' }),
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthLayout
      title="Entrar no GameHub"
      subtitle="Entre para continuar conversando."
      footer={
        <>
          Ainda não tem uma conta? <Link to="/register">Criar conta</Link>
        </>
      }
    >
      <form onSubmit={handleSubmit} noValidate aria-busy={isSubmitting}>
        {sessionExpired && !formError && (
          <Alert variant="warning" role="status">
            Sua sessão expirou. Entre novamente para continuar.
          </Alert>
        )}

        {formError && (
          <Alert variant="error" role="alert">
            {formError}
          </Alert>
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

        <Button type="submit" isLoading={isSubmitting}>
          {isSubmitting ? 'Entrando…' : 'Entrar'}
        </Button>
      </form>
    </AuthLayout>
  )
}

export default LoginPage
