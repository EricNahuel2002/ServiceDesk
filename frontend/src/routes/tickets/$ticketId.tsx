import { createFileRoute, Link } from '@tanstack/react-router'
import { Card } from '../../components/common/Card'
import { Badge } from '../../components/common/Badge'
import { AppShell } from '../../components/layout/AppShell'
import { ChatPanel } from '../../components/chat/ChatPanel'
import { useTickets, useIsTicketClosed } from '../../features/tickets/queries'
import { requireCliente } from '../../features/tickets/auth'
import { formatDate } from '../../utils/format'

export const Route = createFileRoute('/tickets/$ticketId')({
  beforeLoad: () => requireCliente(),
  component: ClientTicketDetailPage,
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

function ClientTicketDetailPage() {
  const { ticketId } = Route.useParams()
  const tickets = useTickets()
  const { isClosed } = useIsTicketClosed()

  if (tickets.isPending) {
    return <AppShell><p className="text-gray-500">Cargando...</p></AppShell>
  }

  const ticket = (tickets.data ?? []).find((t) => t.id === ticketId)

  if (!ticket) {
    return <AppShell><p className="text-gray-500">Ticket no encontrado.</p></AppShell>
  }

  return (
    <AppShell>
      <div className="mb-4">
        <Link
          to="/tickets/mis-tickets"
          className="text-sm font-medium text-[#0F52BA] hover:underline"
        >
          ← Volver
        </Link>
      </div>

      <div className="flex flex-col gap-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{ticket.title}</h1>
        </div>

        <div className="grid gap-6 lg:grid-cols-3">
          <div className="lg:col-span-2 flex flex-col gap-6">
            <Card>
              <div className="flex flex-col gap-4">
                <div>
                  <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                    Descripción
                  </span>
                  <p className="mt-1 text-sm text-gray-900 whitespace-pre-wrap">{ticket.description}</p>
                </div>

                <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
                  <div className="flex flex-col gap-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                      Categoría
                    </span>
                    <span className="text-sm font-medium text-gray-900">{ticket.categoryName}</span>
                  </div>
                  <div className="flex flex-col gap-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                      Estado
                    </span>
                    <div>
                      <Badge color={getStatusBadgeColor(ticket.statusName)}>{ticket.statusName}</Badge>
                    </div>
                  </div>
                  <div className="flex flex-col gap-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                      Prioridad
                    </span>
                    <div>
                      <Badge color={getPriorityBadgeColor(ticket.priorityName)}>{ticket.priorityName}</Badge>
                    </div>
                  </div>
                  <div className="flex flex-col gap-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                      Técnico
                    </span>
                    {ticket.assignedToFirstName ? (
                      <span className="text-sm font-medium text-gray-900">
                        {ticket.assignedToFirstName} {ticket.assignedToLastName}
                      </span>
                    ) : (
                      <span className="text-sm text-gray-400">Sin asignar</span>
                    )}
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="flex flex-col gap-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                      Creado
                    </span>
                    <span className="text-sm font-medium text-gray-900">
                      {formatDate(ticket.createdAtUtc)}
                    </span>
                  </div>
                  {ticket.updatedAtUtc && (
                    <div className="flex flex-col gap-1">
                      <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                        Actualizado
                      </span>
                      <span className="text-sm font-medium text-gray-900">
                        {formatDate(ticket.updatedAtUtc)}
                      </span>
                    </div>
                  )}
                </div>
              </div>
            </Card>

            {!isClosed(ticket) && ticket.assignedToId && (
              <Card>
                <h3 className="mb-3 text-sm font-semibold text-gray-900">Chat con el técnico</h3>
                <ChatPanel ticketId={ticketId} />
              </Card>
            )}
          </div>

          {ticket.attachments.length > 0 && (
            <div className="flex flex-col gap-6">
              <Card>
                <h2 className="mb-4 text-lg font-semibold text-gray-900">Archivos adjuntos</h2>
                <ul className="flex flex-col gap-2">
                  {ticket.attachments.map((att) => (
                    <li
                      key={att.id}
                      className="flex items-center justify-between rounded-md border border-gray-200 px-4 py-2"
                    >
                      <span className="text-sm text-gray-900">{att.fileName}</span>
                      <span className="text-xs text-gray-500">
                        {(att.sizeInBytes / 1024).toFixed(1)} KB
                      </span>
                    </li>
                  ))}
                </ul>
              </Card>
            </div>
          )}
        </div>
      </div>
    </AppShell>
  )
}
