import { createContext } from 'react'
import type { AuthResponse, RegisterRequest } from './types'

export interface AuthContextValue {
  user: AuthResponse['user'] | null
  isAuthenticated: boolean
  isPending: boolean
  login: (email: string, password: string) => Promise<void>
  register: (request: RegisterRequest) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
