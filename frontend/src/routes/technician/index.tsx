import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { Card } from '../../components/common/Card'
import { Badge } from '../../components/common/Badge'
import { TechnicianAppShell } from '../../components/layout/TechnicianAppShell'
import { SlaTicketBadge } from '../../components/common/SlaTicketBadge'
import { useTechnicianTickets, useIsTechnicianTicketClosed } from '../../features/technician/queries'
import { requireTecnico } from '../../features/technician/auth'
import { formatDate } from '../../utils/format'
import { getPriorityLabel, getPriorityBadgeColor } from '../../utils/priority'
import { getStatusBadgeColor } from '../../utils/status'

export const Route = createFileRoute('/technician/')({
  beforeLoad: () => requireTecnico(),
  component: TechnicianDashboardPage,
})

type Tab = 'todos' | 'abiertos' | 'progreso' | 'finalizados'

const tabs: { id: Tab; label: string }[] = [
  { id: 'todos', label: 'Todos' },
  { id: 'abiertos', label: 'Abiertos' },
  { id: 'progreso', label: 'En progreso' },
  { id: 'finalizados', label: 'Finalizados' },
]

function TechnicianDashboardPage() {
  const tickets = useTechnicianTickets()
  const { isClosed } = useIsTechnicianTicketClosed()
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

  const filteredTickets = allTickets.filter((ticket) => {
    if (tab === 'todos') return true
    if (tab === 'finalizados') return isClosed(ticket)
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
    <TechnicianAppShell>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Mis Tickets Asignados</h1>
      </div>

      <div className="mb-6 grid gap-4 sm:grid-cols-3">
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

      {tickets.isPending ? (
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
                  <div className="flex items-center gap-2">
                    {!isClosed(ticket) && (
                      <SlaTicketBadge
                        responseDeadlineAtUtc={ticket.responseDeadlineAtUtc}
                        startedWorkAtUtc={ticket.startedWorkAtUtc}
                        slaPercentageElapsed={ticket.slaPercentageElapsed}
                      />
                    )}
                    <Link
                      to="/technician/tickets/$ticketId"
                      params={{ ticketId: ticket.id }}
                      className="shrink-0 rounded-md bg-[#0F52BA] px-3 py-1.5 text-xs font-medium text-white hover:bg-blue-800"
                    >
                      Ver detalle
                    </Link>
                  </div>
                </div>
                <div className="flex flex-wrap items-center gap-3">
                  <div className="flex items-center gap-1.5">
                    <span className="text-xs text-gray-500">Estado:</span>
                    <Badge color={getStatusBadgeColor(ticket.statusName)}>
                      {ticket.statusName}
                    </Badge>
                  </div>
                  <div className="flex items-center gap-1.5">
                    <span className="text-xs text-gray-500">Prioridad:</span>
                    <Badge color={getPriorityBadgeColor(ticket.priority)}>
                      {getPriorityLabel(ticket.priority)}
                    </Badge>
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
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
                      Solicitante
                    </span>
                    <span className="text-sm font-medium text-gray-900">
                      {ticket.createdById}
                    </span>
                  </div>
                  <div className="flex flex-col gap-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                      Creado
                    </span>
                    <span className="text-sm font-medium text-gray-900">
                      {formatDate(ticket.createdAtUtc)}
                    </span>
                  </div>
                </div>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </TechnicianAppShell>
  )
}
