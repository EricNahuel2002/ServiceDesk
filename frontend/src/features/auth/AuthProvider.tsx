import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { setAccessToken } from '../../lib/apiClient'
import { queryClient } from '../../lib/queryClient'
import { AuthContext } from './context'
import type { AuthContextValue } from './context'
import { useLogin, useLogout, useRegister } from './queries'
import type { AuthResponse, RegisterRequest } from './types'

const STORAGE_KEY = 'servicedesk.auth'

function readStoredSession(): AuthResponse | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as AuthResponse) : null
  } catch {
    return null
  }
}

function persistSession(session: AuthResponse): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
}

function removeStoredSession(): void {
  localStorage.removeItem(STORAGE_KEY)
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthResponse | null>(() => {
    const stored = readStoredSession()
    if (stored) {
      setAccessToken(stored.accessToken)
    }
    return stored
  })

  const loginMutation = useLogin()
  const registerMutation = useRegister()
  const logoutMutation = useLogout()

  const login = useCallback(
    async (email: string, password: string) => {
      const response = await loginMutation.mutateAsync({ email, password })
      setAccessToken(response.accessToken)
      persistSession(response)
      setSession(response)
    },
    [loginMutation],
  )

  const register = useCallback(
    async (request: RegisterRequest) => {
      const response = await registerMutation.mutateAsync(request)
      setAccessToken(response.accessToken)
      persistSession(response)
      setSession(response)
    },
    [registerMutation],
  )

  const logout = useCallback(async () => {
    if (session?.refreshToken) {
      await logoutMutation
        .mutateAsync({ refreshToken: session.refreshToken })
        .catch(() => undefined)
    }
    setAccessToken(null)
    removeStoredSession()
    setSession(null)
    queryClient.clear()
  }, [session, logoutMutation])

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      isAuthenticated: session !== null,
      isPending: loginMutation.isPending || registerMutation.isPending,
      login,
      register,
      logout,
    }),
    [
      session,
      login,
      register,
      logout,
      loginMutation.isPending,
      registerMutation.isPending,
    ],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
