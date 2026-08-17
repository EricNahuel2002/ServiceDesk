import { createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import { Card } from '../../../components/common/Card'
import { Badge } from '../../../components/common/Badge'
import { Input } from '../../../components/common/Input'
import { Button } from '../../../components/common/Button'
import { AdminAppShell } from '../../../components/layout/AdminAppShell'
import { requireAdmin } from '../../../features/admin/auth'
import { useAdminUsers, useCreateUser } from '../../../features/admin/queries'
import { useAuth } from '../../../hooks/useAuth'
import type { UserListItemDto } from '../../../features/admin/types'

export const Route = createFileRoute('/admin/technicians/')({
  beforeLoad: () => requireAdmin(),
  component: AdminTechniciansPage,
})

type Tab = 'all' | 'tecnico' | 'administrador'

const tabs: { id: Tab; label: string }[] = [
  { id: 'all', label: 'Todos' },
  { id: 'tecnico', label: 'Técnicos' },
  { id: 'administrador', label: 'Administradores' },
]

function getRoleBadgeColor(role: string): 'amber' | 'blue' | 'green' {
  if (role === 'Tecnico') return 'amber'
  if (role === 'Administrador') return 'blue'
  return 'green'
}

function getRoleLabel(role: string): string {
  if (role === 'Tecnico') return 'Técnico'
  if (role === 'Administrador') return 'Administrador'
  if (role === 'Cliente') return 'Cliente'
  return role
}

function AdminTechniciansPage() {
  const [tab, setTab] = useState<Tab>('all')

  return (
    <AdminAppShell>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Usuarios</h1>
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

      <UsersTab filter={tab} />
    </AdminAppShell>
  )
}

function UsersTab({ filter }: { filter: Tab }) {
  const users = useAdminUsers()
  const createUser = useCreateUser()
  const { user: currentUser } = useAuth()

  const [showForm, setShowForm] = useState(false)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [role, setRole] = useState('Tecnico')
  const [error, setError] = useState('')

  function resetForm() {
    setShowForm(false)
    setEmail('')
    setPassword('')
    setFirstName('')
    setLastName('')
    setRole('Tecnico')
    setError('')
  }

  function handleSubmit() {
    if (!email.trim()) {
      setError('El email es obligatorio.')
      return
    }
    if (!password.trim()) {
      setError('La contraseña es obligatoria.')
      return
    }
    if (!firstName.trim()) {
      setError('El nombre es obligatorio.')
      return
    }
    if (!lastName.trim()) {
      setError('El apellido es obligatorio.')
      return
    }

    createUser.mutate(
      {
        email: email.trim(),
        password: password.trim(),
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        companyId: currentUser?.companyId ?? '',
        role,
      },
      {
        onSuccess: () => resetForm(),
        onError: (err) => {
          setError(err instanceof Error ? err.message : 'Error al crear el usuario.')
        },
      },
    )
  }

  if (users.isPending) return <p className="text-gray-500">Cargando...</p>

  const filteredUsers = (users.data ?? []).filter((u) => {
    if (filter === 'all') return true
    return u.role.toLowerCase() === filter
  })

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-gray-500">
          {filteredUsers.length} usuario{filteredUsers.length !== 1 ? 's' : ''}
        </p>
        {!showForm && (
          <Button onClick={() => { setShowForm(true); setError('') }}>
            + Nuevo
          </Button>
        )}
      </div>

      {showForm && (
        <Card>
          <h3 className="mb-3 text-sm font-semibold text-gray-900">
            Nuevo usuario
          </h3>
          <div className="flex flex-col gap-3">
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <Input
                label="Nombre"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                placeholder="Nombre"
              />
              <Input
                label="Apellido"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                placeholder="Apellido"
              />
            </div>
            <Input
              label="Email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="usuario@empresa.com"
            />
            <Input
              label="Contraseña"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Mínimo 8 caracteres"
            />
            <div className="flex flex-col gap-1">
              <label htmlFor="role" className="text-sm font-medium text-gray-700">
                Rol
              </label>
              <select
                id="role"
                value={role}
                onChange={(e) => setRole(e.target.value)}
                className="rounded-md border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:border-emerald-500 focus:outline-none"
              >
                <option value="Tecnico">Técnico</option>
                <option value="Administrador">Administrador</option>
              </select>
            </div>
            {error && <p className="text-sm text-red-600">{error}</p>}
            <div className="flex gap-2">
              <Button onClick={handleSubmit} disabled={createUser.isPending}>
                {createUser.isPending ? 'Creando...' : 'Crear usuario'}
              </Button>
              <Button variant="secondary" onClick={resetForm}>
                Cancelar
              </Button>
            </div>
          </div>
        </Card>
      )}

      {filteredUsers.length === 0 ? (
        <p className="text-gray-500">No hay usuarios en esta sección.</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {filteredUsers.map((u) => (
            <UserCard key={u.id} user={u} />
          ))}
        </ul>
      )}
    </div>
  )
}

function UserCard({ user }: { user: UserListItemDto }) {
  return (
    <li>
      <Card className="flex items-center justify-between">
        <div className="flex flex-col gap-0.5">
          <span className="text-sm font-semibold text-gray-900">
            {user.firstName} {user.lastName}
          </span>
          <span className="text-xs text-gray-500">{user.email}</span>
        </div>
        <div className="flex items-center gap-2">
          <Badge color={getRoleBadgeColor(user.role)}>
            {getRoleLabel(user.role)}
          </Badge>
          <Badge color={user.isActive ? 'green' : 'gray'}>
            {user.isActive ? 'Activo' : 'Inactivo'}
          </Badge>
        </div>
      </Card>
    </li>
  )
}
