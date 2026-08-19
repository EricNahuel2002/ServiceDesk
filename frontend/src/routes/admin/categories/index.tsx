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
  useCreateCategory,
  useUpdateCategory,
} from '../../../features/admin/queries'

export const Route = createFileRoute('/admin/categories/')({
  beforeLoad: () => requireAdmin(),
  component: AdminCategoriesPage,
})

function AdminCategoriesPage() {
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
    <AdminAppShell>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Categorías</h1>
      </div>

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
    </AdminAppShell>
  )
}
