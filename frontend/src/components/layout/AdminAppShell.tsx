import { Link } from '@tanstack/react-router'
import type { ReactNode } from 'react'
import { useAuth } from '../../hooks/useAuth'

interface NavItem {
  to: '/admin' | '/admin/catalogs' | '/admin/technicians'
  label: string
}

const navItems: NavItem[] = [
  { to: '/admin', label: 'Dashboard' },
  { to: '/admin/catalogs', label: 'Catálogos' },
  { to: '/admin/technicians', label: 'Usuarios' },
]

export function AdminAppShell({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth()

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-[#0F52BA]">
        <div className="flex h-16 items-center justify-between px-6">
          <Link to="/admin" className="text-lg font-semibold text-white">
            ServiceDesk
          </Link>
          <nav className="flex items-center gap-1">
            {navItems.map((item) => (
              <Link
                key={item.to}
                to={item.to}
                className="rounded-md px-3 py-2 text-sm font-medium text-blue-100 hover:bg-blue-700"
                activeProps={{ className: 'bg-blue-700 text-white' }}
              >
                {item.label}
              </Link>
            ))}
          </nav>
          <div className="flex items-center gap-4">
            <span className="text-sm text-blue-100">
              {user ? `${user.firstName} ${user.lastName}` : ''}
            </span>
            <button
              type="button"
              onClick={() => void logout()}
              className="text-sm text-blue-100 hover:text-white"
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
