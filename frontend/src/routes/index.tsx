import { createFileRoute, Link, Navigate } from '@tanstack/react-router'
import { useAuth } from '../hooks/useAuth'

export const Route = createFileRoute('/')({
  component: HomePage,
})

function HomePage() {
  const { isAuthenticated } = useAuth()

  if (isAuthenticated) {
    return <Navigate to="/dashboard" />
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4">
      <h1 className="text-3xl font-bold text-gray-900">ServiceDesk</h1>
      <p className="text-gray-600">Gestión de incidencias y soporte técnico</p>
      <div className="mt-4 flex gap-3">
        <Link
          to="/login"
          className="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
        >
          Iniciar sesión
        </Link>
        <Link
          to="/register"
          className="rounded-md border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
        >
          Crear cuenta
        </Link>
      </div>
    </div>
  )
}
