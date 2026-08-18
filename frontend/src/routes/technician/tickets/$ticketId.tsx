import { createFileRoute, Link, useNavigate } from '@tanstack/react-router'
import { useState } from 'react'
import { Card } from '../../../components/common/Card'
import { Badge } from '../../../components/common/Badge'
import { Button } from '../../../components/common/Button'
import { TechnicianAppShell } from '../../../components/layout/TechnicianAppShell'
import { ChatPanel } from '../../../components/chat/ChatPanel'
import { useTechnicianTicket, useResolveTicket, useIsTechnicianTicketClosed } from '../../../features/technician/queries'
import { requireTecnico } from '../../../features/technician/auth'
import { formatDate } from '../../../utils/format'

export const Route = createFileRoute('/technician/tickets/$ticketId')({
  beforeLoad: () => requireTecnico(),
  component: TechnicianTicketDetailPage,
})

function getStatusBadgeColor(statusName: string): 'blue' | 'amber' | 'green' | 'red' | 'gray' {
  const lower = statusName.toLowerCase()
  if (lower.includes('nuevo') || lower.includes('abierto') || lower.includes('new') || lower.includes('open'))
    return 'blue'
  if (lower.includes('progreso') || lower.includes('asignad') || lower.includes('progress') || lower.includes('assigned'))
    return 'amber'
  if (lower.includes('resuelto') || lower.includes('finalizado') || lower.includes('closed') || lower.includes('resolved'))
    return 'green'
  if (lower.includes('cancelado') || lower.includes('cerrado') || lower.includes('cancelled') || lower.includes('canceled'))
    return 'red'
  return 'gray'
}

function getPriorityBadgeColor(priorityName: string): 'red' | 'amber' | 'green' | 'gray' {
  const lower = priorityName.toLowerCase()
  if (lower.includes('alta') || lower.includes('high') || lower.includes('urgente'))
    return 'red'
  if (lower.includes('media') || lower.includes('medium') || lower.includes('normal'))
    return 'amber'
  if (lower.includes('baja') || lower.includes('low'))
    return 'green'
  return 'gray'
}

function TechnicianTicketDetailPage() {
  const { ticketId } = Route.useParams()
  const navigate = useNavigate()
  const ticket = useTechnicianTicket(ticketId)
  const resolveTicket = useResolveTicket()
  const { isClosed } = useIsTechnicianTicketClosed()

  const [showResolveForm, setShowResolveForm] = useState(false)
  const [resolutionNote, setResolutionNote] = useState('')
  const [error, setError] = useState('')

  function handleResolve() {
    resolveTicket.mutate(
      { id: ticketId, resolutionNote: resolutionNote.trim() },
      {
        onSuccess: () => {
          void navigate({ to: '/technician' })
        },
        onError: (err) => {
          setError(err instanceof Error ? err.message : 'Error al resolver el ticket.')
        },
      },
    )
  }

  if (ticket.isPending) return <TechnicianAppShell><p className="text-gray-500">Cargando...</p></TechnicianAppShell>

  if (!ticket.data) return <TechnicianAppShell><p className="text-gray-500">Ticket no encontrado.</p></TechnicianAppShell>

  const t = ticket.data

  return (
    <TechnicianAppShell>
      <div className="mb-4">
        <Link
          to="/technician"
          className="text-sm font-medium text-[#0F52BA] hover:underline"
        >
          ← Volver
        </Link>
      </div>

      <div className="flex flex-col gap-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{t.title}</h1>
        </div>

        <Card>
          <div className="flex flex-col gap-4">
            <div>
              <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                Descripción
              </span>
              <p className="mt-1 text-sm text-gray-900">{t.description}</p>
            </div>

            <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
              <div className="flex flex-col gap-1">
                <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                  Categoría
                </span>
                <span className="text-sm font-medium text-gray-900">{t.categoryName}</span>
              </div>
              <div className="flex flex-col gap-1">
                <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                  Estado
                </span>
                <div>
                  <Badge color={getStatusBadgeColor(t.statusName)}>{t.statusName}</Badge>
                </div>
              </div>
              <div className="flex flex-col gap-1">
                <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                  Prioridad
                </span>
                <div>
                  <Badge color={getPriorityBadgeColor(t.priorityName)}>{t.priorityName}</Badge>
                </div>
              </div>
              <div className="flex flex-col gap-1">
                <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                  Solicitante
                </span>
                <span className="text-sm font-medium text-gray-900">
                  {t.createdById}
                </span>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="flex flex-col gap-1">
                <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                  Creado
                </span>
                <span className="text-sm font-medium text-gray-900">
                  {formatDate(t.createdAtUtc)}
                </span>
              </div>
              {t.updatedAtUtc && (
                <div className="flex flex-col gap-1">
                  <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                    Actualizado
                  </span>
                  <span className="text-sm font-medium text-gray-900">
                    {formatDate(t.updatedAtUtc)}
                  </span>
                </div>
              )}
            </div>
          </div>
        </Card>

        {!isClosed(t) && (
          <Card>
            <h3 className="mb-3 text-sm font-semibold text-gray-900">Chat con el cliente</h3>
            <ChatPanel ticketId={ticketId} />
          </Card>
        )}

        {!isClosed(t) && (
          <Card>
            <h3 className="mb-3 text-sm font-semibold text-gray-900">Acciones</h3>
            {!showResolveForm ? (
              <Button onClick={() => { setShowResolveForm(true); setError('') }}>
                Resolver ticket
              </Button>
            ) : (
              <div className="flex flex-col gap-3">
                <div className="flex flex-col gap-1">
                  <label htmlFor="resolutionNote" className="text-sm font-medium text-gray-700">
                    Nota de resolución (opcional)
                  </label>
                  <textarea
                    id="resolutionNote"
                    value={resolutionNote}
                    onChange={(e) => setResolutionNote(e.target.value)}
                    rows={3}
                    placeholder="Describe cómo se resolvió el problema..."
                    className="rounded-md border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder:text-gray-400 focus:border-emerald-500 focus:outline-none"
                  />
                </div>
                {error && <p className="text-sm text-red-600">{error}</p>}
                <div className="flex gap-2">
                  <Button
                    onClick={handleResolve}
                    disabled={resolveTicket.isPending}
                  >
                    {resolveTicket.isPending ? 'Resolviendo...' : 'Confirmar resolución'}
                  </Button>
                  <Button
                    variant="secondary"
                    onClick={() => { setShowResolveForm(false); setResolutionNote(''); setError('') }}
                  >
                    Cancelar
                  </Button>
                </div>
              </div>
            )}
          </Card>
        )}
      </div>
    </TechnicianAppShell>
  )
}
