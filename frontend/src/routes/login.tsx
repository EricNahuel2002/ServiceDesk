import {
  createFileRoute,
  Link,
  Navigate,
} from '@tanstack/react-router'
import { useState, type FormEvent } from 'react'
import { Button } from '../components/common/Button'
import { Input } from '../components/common/Input'
import { useAuth } from '../hooks/useAuth'

export const Route = createFileRoute('/login')({
  component: LoginPage,
})

function LoginPage() {
  const { login, isAuthenticated, isPending, user } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    try {
      await login(email, password)
    } catch {
      setError('Credenciales inválidas.')
    }
  }

  if (isAuthenticated) {
    if (user?.role === 'Administrador') {
      return <Navigate to="/admin" />
    }
    if (user?.role === 'Tecnico') {
      return <Navigate to="/technician" />
    }
    return <Navigate to="/tickets" />
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50">
      <div className="w-full max-w-sm rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
        <h1 className="mb-6 text-xl font-bold text-gray-900">Iniciar sesión</h1>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
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
            autoComplete="current-password"
            required
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
          {error ? <p className="text-sm text-red-600">{error}</p> : null}
          <Button type="submit" disabled={isPending}>
            {isPending ? 'Ingresando...' : 'Ingresar'}
          </Button>
        </form>
        <p className="mt-4 text-sm text-gray-500">
          ¿No tenés cuenta?{' '}
          <Link to="/register" className="text-emerald-600 hover:underline">
            Registrate
          </Link>
        </p>
      </div>
    </div>
  )
}
