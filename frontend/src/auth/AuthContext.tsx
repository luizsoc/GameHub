import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import * as authApi from '../api/auth'
import type { AuthUser } from '../types/auth'
import { onUnauthorized } from './authEvents'
import { decodeJwt, isExpired } from './jwt'
import { getToken, removeToken, setToken } from './tokenStorage'
import { AuthContext, type AuthContextValue } from './useAuth'

interface Session {
  token: string
  user: AuthUser
  expiresAt: number
}

// setTimeout overflows above ~24.8 days and would fire immediately.
const MAX_TIMEOUT_MS = 2_147_483_647

function createSession(token: string): Session | null {
  const claims = decodeJwt(token)

  if (!claims || isExpired(claims)) {
    return null
  }

  return {
    token,
    expiresAt: claims.exp * 1000,
    user: {
      id: claims.sub,
      username: claims.unique_name,
      email: claims.email,
    },
  }
}

// Reading localStorage is synchronous, so the session is restored while the
// state is initialized instead of in an effect (no extra render, no flicker).
function restoreSession(): Session | null {
  const storedToken = getToken()

  if (!storedToken) {
    return null
  }

  const session = createSession(storedToken)

  if (!session) {
    removeToken()
  }

  return session
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(restoreSession)
  // True when the session ended on its own (token expired or rejected by the
  // API) rather than through logout, so the login page can say why.
  const [sessionExpired, setSessionExpired] = useState(false)

  // Restoration is synchronous today, so there is never a loading phase.
  // Kept in the context so consumers are ready if it ever becomes async.
  const isLoading = false

  const logout = useCallback(() => {
    removeToken()
    setSession(null)
    setSessionExpired(false)
  }, [])

  // The HTTP client already removed the token; just reflect it in state.
  useEffect(
    () =>
      onUnauthorized(() => {
        setSession(null)
        setSessionExpired(true)
      }),
    [],
  )

  // Drop the token locally once it expires.
  useEffect(() => {
    if (!session) {
      return
    }

    const timeout = setTimeout(
      () => {
        removeToken()
        setSession(null)
        setSessionExpired(true)
      },
      Math.min(session.expiresAt - Date.now(), MAX_TIMEOUT_MS),
    )

    return () => clearTimeout(timeout)
  }, [session])

  const login = useCallback(async (email: string, password: string) => {
    const { token } = await authApi.login({ email, password })
    const newSession = createSession(token)

    if (!newSession) {
      throw new Error('Received an invalid token from the server.')
    }

    setToken(token)
    setSession(newSession)
    setSessionExpired(false)
  }, [])

  // The register endpoint does not return a token, so log in right after.
  const register = useCallback(
    async (username: string, email: string, password: string) => {
      await authApi.register({ username, email, password })
      await login(email, password)
    },
    [login],
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      token: session?.token ?? null,
      isAuthenticated: session !== null,
      isLoading,
      sessionExpired,
      login,
      register,
      logout,
    }),
    [session, isLoading, sessionExpired, login, register, logout],
  )

  return <AuthContext value={value}>{children}</AuthContext>
}
