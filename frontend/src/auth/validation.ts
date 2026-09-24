// Basic client-side checks only. The backend remains the validation authority.

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function validateEmail(email: string): string | undefined {
  if (!email.trim()) {
    return 'Informe o e-mail.'
  }

  if (!EMAIL_PATTERN.test(email.trim())) {
    return 'Informe um e-mail válido.'
  }

  return undefined
}

export function validatePassword(password: string): string | undefined {
  return password ? undefined : 'Informe a senha.'
}

export function validateUsername(username: string): string | undefined {
  return username.trim() ? undefined : 'Informe o nome de usuário.'
}
