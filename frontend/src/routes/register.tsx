import {
  createFileRoute,
  Link,
  Navigate,
  useNavigate,
} from '@tanstack/react-router'
import { useState, type FormEvent } from 'react'
import { Button } from '../components/common/Button'
import { Input } from '../components/common/Input'
import type { RegisterRequest } from '../features/auth/types'
import { useAuth } from '../hooks/useAuth'

export const Route = createFileRoute('/register')({
  component: RegisterPage,
})

function RegisterPage() {
  const navigate = useNavigate()
  const { register, isAuthenticated, isPending } = useAuth()
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [companyId, setCompanyId] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    const request: RegisterRequest = {
      firstName,
      lastName,
      email,
      password,
      companyId,
    }
    try {
      await register(request)
      await navigate({ to: '/tickets' })
    } catch {
      setError('No se pudo completar el registro.')
    }
  }

  if (isAuthenticated) {
    return <Navigate to="/tickets" />
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50">
      <div className="w-full max-w-sm rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
        <h1 className="mb-6 text-xl font-bold text-gray-900">Crear cuenta</h1>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <Input
            label="Nombre"
            name="firstName"
            required
            value={firstName}
            onChange={(event) => setFirstName(event.target.value)}
          />
          <Input
            label="Apellido"
            name="lastName"
            required
            value={lastName}
            onChange={(event) => setLastName(event.target.value)}
          />
          <Input
            label="Email"
            name="email"
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
          <Input
            label="Contraseña"
            name="password"
            type="password"
            autoComplete="new-password"
            required
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
          <Input
            label="ID de empresa"
            name="companyId"
            required
            placeholder="00000000-0000-0000-0000-000000000000"
            value={companyId}
            onChange={(event) => setCompanyId(event.target.value)}
          />
          {error ? <p className="text-sm text-red-600">{error}</p> : null}
          <Button type="submit" disabled={isPending}>
            {isPending ? 'Registrando...' : 'Registrarse'}
          </Button>
        </form>
        <p className="mt-4 text-sm text-gray-500">
          ¿Ya tenés cuenta?{' '}
          <Link to="/login" className="text-emerald-600 hover:underline">
            Iniciá sesión
          </Link>
        </p>
      </div>
    </div>
  )
}
