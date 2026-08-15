import { createFileRoute } from '@tanstack/react-router'
import { Card } from '../components/common/Card'
import { AppShell } from '../components/layout/AppShell'
import { useCategories, usePriorities, useStatuses } from '../features/catalog/queries'
import { useAuth } from '../hooks/useAuth'

export const Route = createFileRoute('/dashboard')({
  component: DashboardPage,
})

function DashboardPage() {
  const { user } = useAuth()
  const categories = useCategories()
  const priorities = usePriorities()
  const statuses = useStatuses()

  return (
    <AppShell>
      <h1 className="mb-6 text-2xl font-bold text-gray-900">Dashboard</h1>
      <p className="mb-6 text-gray-600">
        Bienvenido, {user ? user.firstName : 'usuario'}.
      </p>
      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <p className="text-sm text-gray-500">Categorías</p>
          <p className="mt-1 text-2xl font-semibold text-gray-900">
            {categories.data?.length ?? 0}
          </p>
        </Card>
        <Card>
          <p className="text-sm text-gray-500">Prioridades</p>
          <p className="mt-1 text-2xl font-semibold text-gray-900">
            {priorities.data?.length ?? 0}
          </p>
        </Card>
        <Card>
          <p className="text-sm text-gray-500">Estados</p>
          <p className="mt-1 text-2xl font-semibold text-gray-900">
            {statuses.data?.length ?? 0}
          </p>
        </Card>
      </div>
    </AppShell>
  )
}
