import { createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import { Card } from '../../../components/common/Card'
import { Badge } from '../../../components/common/Badge'
import { Input } from '../../../components/common/Input'
import { Button } from '../../../components/common/Button'
import { AdminAppShell } from '../../../components/layout/AdminAppShell'
import { requireAdmin } from '../../../features/admin/auth'
import {
  useAdminCategories,
  useAdminStatuses,
  useCreateCategory,
  useUpdateCategory,
  useCreateStatus,
  useUpdateStatus,
} from '../../../features/admin/queries'

export const Route = createFileRoute('/admin/catalogs/')({
  beforeLoad: () => requireAdmin(),
  component: AdminCatalogsPage,
})

type Tab = 'categories' | 'statuses'

const tabs: { id: Tab; label: string }[] = [
  { id: 'categories', label: 'Categorías' },
  { id: 'statuses', label: 'Estados' },
]

function AdminCatalogsPage() {
  const [tab, setTab] = useState<Tab>('categories')

  return (
    <AdminAppShell>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Catálogos</h1>
      </div>

      <div className="mb-4 flex gap-1 rounded-lg border border-gray-200 bg-gray-100 p-1">
        {tabs.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setTab(item.id)}
            className={`flex-1 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
              tab === item.id
                ? 'bg-[#0F52BA] text-white'
                : 'text-gray-600 hover:bg-gray-200'
            }`}
          >
            {item.label}
          </button>
        ))}
      </div>

      {tab === 'categories' && <CategoriesTab />}
      {tab === 'statuses' && <StatusesTab />}
    </AdminAppShell>
  )
}

function CategoriesTab() {
  const categories = useAdminCategories()
  const createCategory = useCreateCategory()
  const updateCategory = useUpdateCategory()

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [error, setError] = useState('')

  function resetForm() {
    setShowForm(false)
    setEditingId(null)
    setName('')
    setIsActive(true)
    setError('')
  }

  function handleEdit(cat: { id: string; name: string; isActive: boolean }) {
    setEditingId(cat.id)
    setName(cat.name)
    setIsActive(cat.isActive)
    setShowForm(true)
    setError('')
  }

  function handleSubmit() {
    if (!name.trim()) {
      setError('El nombre es obligatorio.')
      return
    }

    if (editingId) {
      updateCategory.mutate(
        { id: editingId, data: { name: name.trim(), isActive } },
        {
          onSuccess: () => resetForm(),
          onError: (err) => {
            setError(err instanceof Error ? err.message : 'Error al guardar.')
          },
        },
      )
    } else {
      createCategory.mutate(
        { name: name.trim() },
        {
          onSuccess: () => resetForm(),
          onError: (err) => {
            setError(err instanceof Error ? err.message : 'Error al crear.')
          },
        },
      )
    }
  }

  if (categories.isPending) return <p className="text-gray-500">Cargando...</p>

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-gray-500">
          {categories.data?.length ?? 0} categorías
        </p>
        {!showForm && (
          <Button onClick={() => { setShowForm(true); setEditingId(null); setError('') }}>
            + Nuevo
          </Button>
        )}
      </div>

      {showForm && (
        <Card>
          <h3 className="mb-3 text-sm font-semibold text-gray-900">
            {editingId ? 'Editar categoría' : 'Nueva categoría'}
          </h3>
          <div className="flex flex-col gap-3">
            <Input
              label="Nombre"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Nombre de la categoría"
            />
            {editingId && (
              <label className="flex items-center gap-2 text-sm text-gray-700">
                <input
                  type="checkbox"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300"
                />
                Activo
              </label>
            )}
            {error && <p className="text-sm text-red-600">{error}</p>}
            <div className="flex gap-2">
              <Button
                onClick={handleSubmit}
                disabled={createCategory.isPending || updateCategory.isPending}
              >
                {createCategory.isPending || updateCategory.isPending
                  ? 'Guardando...'
                  : 'Guardar'}
              </Button>
              <Button variant="secondary" onClick={resetForm}>
                Cancelar
              </Button>
            </div>
          </div>
        </Card>
      )}

      {categories.data?.length === 0 ? (
        <p className="text-gray-500">No hay categorías.</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {categories.data?.map((cat) => (
            <li key={cat.id}>
              <Card className="flex items-center justify-between">
                <div className="flex flex-col gap-0.5">
                  <span className="text-sm font-semibold text-gray-900">{cat.name}</span>
                  <span className="text-xs text-gray-500">Categoría de ticket</span>
                </div>
                <div className="flex items-center gap-2">
                  <Badge color={cat.isActive ? 'green' : 'gray'}>
                    {cat.isActive ? 'Activo' : 'Inactivo'}
                  </Badge>
                  <Button
                    variant="secondary"
                    onClick={() => handleEdit(cat)}
                    className="text-xs"
                  >
                    Editar
                  </Button>
                </div>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

function StatusesTab() {
  const statuses = useAdminStatuses()
  const createStatus = useCreateStatus()
  const updateStatus = useUpdateStatus()

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [sortOrder, setSortOrder] = useState(0)
  const [isClosed, setIsClosed] = useState(false)
  const [isActive, setIsActive] = useState(true)
  const [error, setError] = useState('')

  function resetForm() {
    setShowForm(false)
    setEditingId(null)
    setName('')
    setSortOrder(0)
    setIsClosed(false)
    setIsActive(true)
    setError('')
  }

  function handleEdit(item: { id: string; name: string; sortOrder: number; isClosed: boolean; isActive: boolean }) {
    setEditingId(item.id)
    setName(item.name)
    setSortOrder(item.sortOrder)
    setIsClosed(item.isClosed)
    setIsActive(item.isActive)
    setShowForm(true)
    setError('')
  }

  function handleSubmit() {
    if (!name.trim()) {
      setError('El nombre es obligatorio.')
      return
    }

    if (editingId) {
      updateStatus.mutate(
        { id: editingId, data: { name: name.trim(), sortOrder, isClosed, isActive } },
        {
          onSuccess: () => resetForm(),
          onError: (err) => {
            setError(err instanceof Error ? err.message : 'Error al guardar.')
          },
        },
      )
    } else {
      createStatus.mutate(
        { name: name.trim(), sortOrder, isClosed },
        {
          onSuccess: () => resetForm(),
          onError: (err) => {
            setError(err instanceof Error ? err.message : 'Error al crear.')
          },
        },
      )
    }
  }

  if (statuses.isPending) return <p className="text-gray-500">Cargando...</p>

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-gray-500">
          {statuses.data?.length ?? 0} estados
        </p>
        {!showForm && (
          <Button onClick={() => { setShowForm(true); setEditingId(null); setError('') }}>
            + Nuevo
          </Button>
        )}
      </div>

      {showForm && (
        <Card>
          <h3 className="mb-3 text-sm font-semibold text-gray-900">
            {editingId ? 'Editar estado' : 'Nuevo estado'}
          </h3>
          <div className="flex flex-col gap-3">
            <Input
              label="Nombre"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Nombre del estado"
            />
            <Input
              label="Orden"
              type="number"
              value={sortOrder}
              onChange={(e) => setSortOrder(Number(e.target.value))}
              min={0}
            />
            <label className="flex items-center gap-2 text-sm text-gray-700">
              <input
                type="checkbox"
                checked={isClosed}
                onChange={(e) => setIsClosed(e.target.checked)}
                className="h-4 w-4 rounded border-gray-300"
              />
              Estado cerrado
            </label>
            {editingId && (
              <label className="flex items-center gap-2 text-sm text-gray-700">
                <input
                  type="checkbox"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300"
                />
                Activo
              </label>
            )}
            {error && <p className="text-sm text-red-600">{error}</p>}
            <div className="flex gap-2">
              <Button
                onClick={handleSubmit}
                disabled={createStatus.isPending || updateStatus.isPending}
              >
                {createStatus.isPending || updateStatus.isPending
                  ? 'Guardando...'
                  : 'Guardar'}
              </Button>
              <Button variant="secondary" onClick={resetForm}>
                Cancelar
              </Button>
            </div>
          </div>
        </Card>
      )}

      {statuses.data?.length === 0 ? (
        <p className="text-gray-500">No hay estados.</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {statuses.data?.map((item) => (
            <li key={item.id}>
              <Card className="flex items-center justify-between">
                <div className="flex flex-col gap-0.5">
                  <span className="text-sm font-semibold text-gray-900">{item.name}</span>
                  <span className="text-xs text-gray-500">Flujo de trabajo: {item.sortOrder}</span>
                </div>
                <div className="flex items-center gap-2">
                  {item.isClosed && <Badge color="red">Cerrado</Badge>}
                  <Badge color={item.isActive ? 'green' : 'gray'}>
                    {item.isActive ? 'Activo' : 'Inactivo'}
                  </Badge>
                  <Button
                    variant="secondary"
                    onClick={() => handleEdit(item)}
                    className="text-xs"
                  >
                    Editar
                  </Button>
                </div>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
