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
import AuthLayout from '../components/layout/AuthLayout'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'

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
      // AuthController answers 409 only for a duplicate username or e-mail.
      setFormError(
        getErrorMessage(error, {
          409: 'Este nome de usuário ou e-mail já está em uso.',
        }),
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthLayout
      title="Criar sua conta"
      subtitle="Entre na comunidade GameHub."
      footer={
        <>
          Já possui uma conta? <Link to="/login">Entrar</Link>
        </>
      }
    >
      <form onSubmit={handleSubmit} noValidate aria-busy={isSubmitting}>
        {formError && (
          <Alert variant="error" role="alert">
            {formError}
          </Alert>
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

        <Button type="submit" isLoading={isSubmitting}>
          {isSubmitting ? 'Criando…' : 'Criar conta'}
        </Button>
      </form>
    </AuthLayout>
  )
}

export default RegisterPage
