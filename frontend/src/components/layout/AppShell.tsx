import { Link } from '@tanstack/react-router'
import type { ReactNode } from 'react'
import { useAuth } from '../../hooks/useAuth'

interface NavItem {
  to: '/dashboard' | '/tickets'
  label: string
}

const navItems: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/tickets', label: 'Tickets' },
]

export function AppShell({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth()

  return (
    <div className="flex min-h-screen">
      <aside className="flex w-64 flex-col border-r border-gray-200 bg-gray-50">
        <div className="px-4 py-4 text-lg font-semibold text-gray-900">
          ServiceDesk
        </div>
        <nav className="flex flex-col gap-1 px-2">
          {navItems.map((item) => (
            <Link
              key={item.to}
              to={item.to}
              className="rounded-md px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-100"
              activeProps={{ className: 'bg-gray-200 text-gray-900' }}
            >
              {item.label}
            </Link>
          ))}
        </nav>
      </aside>
      <div className="flex flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-gray-200 px-6 py-3">
          <span className="text-sm text-gray-600">
            {user ? `${user.firstName} ${user.lastName}` : ''}
          </span>
          <button
            type="button"
            onClick={() => void logout()}
            className="text-sm text-gray-600 hover:text-gray-900"
          >
            Cerrar sesión
          </button>
        </header>
        <main className="flex-1 p-6">{children}</main>
      </div>
    </div>
  )
}
