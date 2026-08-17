import { redirect } from '@tanstack/react-router'
import type { AuthResponse } from '../auth/types'

const STORAGE_KEY = 'servicedesk.auth'

function readStoredSession(): AuthResponse | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as AuthResponse) : null
  } catch {
    return null
  }
}

export function requireTecnico() {
  const session = readStoredSession()
  if (!session || session.user.role !== 'Tecnico') {
    throw redirect({ to: '/login' })
  }
}
