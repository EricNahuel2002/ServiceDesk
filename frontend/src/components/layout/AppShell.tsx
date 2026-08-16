import { Link } from '@tanstack/react-router'
import type { ReactNode } from 'react'
import { useAuth } from '../../hooks/useAuth'

interface NavItem {
  to: '/tickets' | '/tickets/mis-tickets'
  label: string
}

const navItems: NavItem[] = [
  { to: '/tickets', label: 'Nuevo ticket' },
  { to: '/tickets/mis-tickets', label: 'Mis tickets' },
]

export function AppShell({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth()

  return (
    <div className="min-h-screen bg-white">
      <header className="bg-emerald-500">
        <div className="flex h-16 items-center justify-between px-6">
          <Link to="/tickets" className="text-lg font-semibold text-white">
            ServiceDesk
          </Link>
          <nav className="flex items-center gap-1">
            {navItems.map((item) => (
              <Link
                key={item.to}
                to={item.to}
                className="rounded-md px-3 py-2 text-sm font-medium text-emerald-50 hover:bg-emerald-600"
                activeProps={{ className: 'bg-emerald-600 text-white' }}
              >
                {item.label}
              </Link>
            ))}
          </nav>
          <div className="flex items-center gap-4">
            <span className="text-sm text-emerald-50">
              {user ? `${user.firstName} ${user.lastName}` : ''}
            </span>
            <button
              type="button"
              onClick={() => void logout()}
              className="text-sm text-emerald-50 hover:text-white"
            >
              Cerrar sesión
            </button>
          </div>
        </div>
      </header>
      <main className="p-6">{children}</main>
    </div>
  )
}
