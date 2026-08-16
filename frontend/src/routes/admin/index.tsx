import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { Card } from '../../components/common/Card'
import { Badge } from '../../components/common/Badge'
import { AdminAppShell } from '../../components/layout/AdminAppShell'
import { useAdminTickets, useIsTicketClosed } from '../../features/admin/queries'
import { formatDate } from '../../utils/format'

export const Route = createFileRoute('/admin/')({
  component: AdminDashboardPage,
})

type Tab = 'todos' | 'abiertos' | 'progreso' | 'finalizados' | 'cancelados'

const tabs: { id: Tab; label: string }[] = [
  { id: 'todos', label: 'Todos' },
  { id: 'abiertos', label: 'Abiertos' },
  { id: 'progreso', label: 'En progreso' },
  { id: 'finalizados', label: 'Finalizados' },
  { id: 'cancelados', label: 'Cancelados' },
]

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

function AdminDashboardPage() {
  const tickets = useAdminTickets()
  const { isClosed, statusesPending } = useIsTicketClosed()
  const [tab, setTab] = useState<Tab>('todos')

  const allTickets = tickets.data ?? []

  const openCount = allTickets.filter((t) => {
    const lower = t.statusName.toLowerCase()
    return (
      !isClosed(t) &&
      !lower.includes('progreso') &&
      !lower.includes('asignad') &&
      !lower.includes('progress') &&
      !lower.includes('assigned')
    )
  }).length

  const inProgressCount = allTickets.filter((t) => {
    const lower = t.statusName.toLowerCase()
    return (
      !isClosed(t) &&
      (lower.includes('progreso') ||
        lower.includes('asignad') ||
        lower.includes('progress') ||
        lower.includes('assigned'))
    )
  }).length

  const closedCount = allTickets.filter((t) => isClosed(t)).length

  const cancelledCount = allTickets.filter((t) => {
    const lower = t.statusName.toLowerCase()
    return (
      isClosed(t) &&
      (lower.includes('cancelado') || lower.includes('cancelled') || lower.includes('canceled'))
    )
  }).length

  const filteredTickets = allTickets.filter((ticket) => {
    if (tab === 'todos') return true
    if (tab === 'finalizados') return isClosed(ticket)
    if (tab === 'cancelados') {
      const lower = ticket.statusName.toLowerCase()
      return (
        isClosed(ticket) &&
        (lower.includes('cancelado') || lower.includes('cancelled') || lower.includes('canceled'))
      )
    }
    if (tab === 'abiertos') {
      const lower = ticket.statusName.toLowerCase()
      return (
        !isClosed(ticket) &&
        !lower.includes('progreso') &&
        !lower.includes('asignad') &&
        !lower.includes('progress') &&
        !lower.includes('assigned')
      )
    }
    if (tab === 'progreso') {
      const lower = ticket.statusName.toLowerCase()
      return (
        !isClosed(ticket) &&
        (lower.includes('progreso') ||
          lower.includes('asignad') ||
          lower.includes('progress') ||
          lower.includes('assigned'))
      )
    }
    return true
  })

  return (
    <AdminAppShell>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Panel de Administración</h1>
      </div>

      <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
        <Card>
          <p className="text-sm text-gray-500">Total</p>
          <p className="mt-1 text-2xl font-semibold text-gray-900">{allTickets.length}</p>
        </Card>
        <Card>
          <p className="text-sm text-gray-500">Abiertos</p>
          <p className="mt-1 text-2xl font-semibold text-[#0F52BA]">{openCount}</p>
        </Card>
        <Card>
          <p className="text-sm text-gray-500">En progreso</p>
          <p className="mt-1 text-2xl font-semibold text-amber-600">{inProgressCount}</p>
        </Card>
        <Card>
          <p className="text-sm text-gray-500">Finalizados</p>
          <p className="mt-1 text-2xl font-semibold text-green-600">{closedCount}</p>
        </Card>
        <Card>
          <p className="text-sm text-gray-500">Cancelados</p>
          <p className="mt-1 text-2xl font-semibold text-red-600">{cancelledCount}</p>
        </Card>
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

      {tickets.isPending || statusesPending ? (
        <p className="text-gray-500">Cargando...</p>
      ) : filteredTickets.length === 0 ? (
        <p className="text-gray-500">No hay tickets en esta sección.</p>
      ) : (
        <ul className="flex flex-col gap-3">
          {filteredTickets.map((ticket) => (
            <li key={ticket.id}>
              <Card className="flex flex-col gap-3">
                <div className="flex items-start justify-between">
                  <p className="font-semibold text-gray-900">{ticket.title}</p>
                  <Link
                    to="/admin/tickets/$ticketId"
                    params={{ ticketId: ticket.id }}
                    className="shrink-0 rounded-md bg-[#0F52BA] px-3 py-1.5 text-xs font-medium text-white hover:bg-blue-800"
                  >
                    Ver detalle
                  </Link>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  <Badge color={getStatusBadgeColor(ticket.statusName)}>
                    {ticket.statusName}
                  </Badge>
                  <Badge color={getPriorityBadgeColor(ticket.priorityName)}>
                    {ticket.priorityName}
                  </Badge>
                </div>
                <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
                  <div className="flex flex-col gap-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                      Categoría
                    </span>
                    <span className="text-sm font-medium text-gray-900">
                      {ticket.categoryName}
                    </span>
                  </div>
                  <div className="flex flex-col gap-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                      Técnico
                    </span>
                    {ticket.assignedToFirstName ? (
                      <>
                        <span className="text-sm font-medium text-gray-900">
                          {ticket.assignedToFirstName} {ticket.assignedToLastName}
                        </span>
                        <span className="text-xs text-gray-500">{ticket.assignedToEmail}</span>
                      </>
                    ) : (
                      <span className="text-sm text-gray-400">Sin asignar</span>
                    )}
                  </div>
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
              </Card>
            </li>
          ))}
        </ul>
      )}
    </AdminAppShell>
  )
}
