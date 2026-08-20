import { useContext } from 'react'
import { AuthContext } from '../features/auth/context'
import type { AuthContextValue } from '../features/auth/context'

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (context === null) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
