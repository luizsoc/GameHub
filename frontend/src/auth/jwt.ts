// Claims emitted by GameHub.Infrastructure.Security.JwtService.
export interface JwtClaims {
  sub: string
  unique_name: string
  email: string
  exp: number
}

// Decodes the JWT payload WITHOUT verifying its signature.
// The backend remains the only authority on whether a token is valid;
// the frontend uses these claims only for display and to drop expired tokens.
export function decodeJwt(token: string): JwtClaims | null {
  const payload = token.split('.')[1]

  if (!payload) {
    return null
  }

  try {
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')
    const bytes = Uint8Array.from(atob(padded), (char) => char.charCodeAt(0))
    const claims: unknown = JSON.parse(new TextDecoder().decode(bytes))

    return isJwtClaims(claims) ? claims : null
  } catch {
    return null
  }
}

export function isExpired(claims: JwtClaims): boolean {
  return claims.exp * 1000 <= Date.now()
}

function isJwtClaims(value: unknown): value is JwtClaims {
  return (
    typeof value === 'object' &&
    value !== null &&
    'sub' in value &&
    typeof value.sub === 'string' &&
    'unique_name' in value &&
    typeof value.unique_name === 'string' &&
    'email' in value &&
    typeof value.email === 'string' &&
    'exp' in value &&
    typeof value.exp === 'number'
  )
}
