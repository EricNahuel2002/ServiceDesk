import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { Card } from '../../components/common/Card'
import { AppShell } from '../../components/layout/AppShell'
import { TicketFields } from '../../components/tickets/TicketFields'
import { useTickets, useIsTicketClosed } from '../../features/tickets/queries'
import { requireCliente } from '../../features/tickets/auth'

export const Route = createFileRoute('/tickets/mis-tickets')({
  beforeLoad: () => requireCliente(),
  component: MyTicketsPage,
})

type Tab = 'todos' | 'activos' | 'finalizados'

const tabs: { id: Tab; label: string }[] = [
  { id: 'todos', label: 'Todos' },
  { id: 'activos', label: 'Activos' },
  { id: 'finalizados', label: 'Finalizados' },
]

function MyTicketsPage() {
  const tickets = useTickets()
  const { isClosed, statusesPending } = useIsTicketClosed()
  const [tab, setTab] = useState<Tab>('todos')

  const allTickets = tickets.data ?? []
  const activeCount = allTickets.filter((ticket) => !isClosed(ticket)).length
  const closedCount = allTickets.length - activeCount

  const filteredTickets =
    tab === 'todos'
      ? allTickets
      : allTickets.filter((ticket) =>
          tab === 'activos' ? !isClosed(ticket) : isClosed(ticket),
        )

  return (
    <AppShell>
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Mis tickets</h1>
        <Link
          to="/tickets"
          className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-600"
        >
          Nuevo ticket
        </Link>
      </div>

      <div className="mb-6 grid gap-4 sm:grid-cols-3">
        <Card>
          <p className="text-sm text-gray-500">Total</p>
          <p className="mt-1 text-2xl font-semibold text-gray-900">{allTickets.length}</p>
        </Card>
        <Card>
          <p className="text-sm text-gray-500">Activos</p>
          <p className="mt-1 text-2xl font-semibold text-emerald-600">{activeCount}</p>
        </Card>
        <Card>
          <p className="text-sm text-gray-500">Finalizados</p>
          <p className="mt-1 text-2xl font-semibold text-gray-900">{closedCount}</p>
        </Card>
      </div>

      <div className="mb-4 flex gap-1 rounded-lg border border-gray-200 bg-gray-50 p-1">
        {tabs.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setTab(item.id)}
            className={`flex-1 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
              tab === item.id
                ? 'bg-emerald-500 text-white'
                : 'text-gray-600 hover:bg-gray-100'
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
                <p className="font-semibold text-gray-900">{ticket.title}</p>
                <TicketFields
                  category={ticket.categoryName}
                  status={ticket.statusName}
                  statusClosed={isClosed(ticket)}
                  createdAt={ticket.createdAtUtc}
                  assignedToFirstName={ticket.assignedToFirstName}
                  assignedToLastName={ticket.assignedToLastName}
                  assignedToEmail={ticket.assignedToEmail}
                  action={
                    ticket.assignedToId ? (
                      <Link
                        to="/tickets/$ticketId"
                        params={{ ticketId: ticket.id }}
                        className="inline-flex w-full items-center justify-center rounded-md bg-emerald-500 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-emerald-600"
                      >
                        Ver chat
                      </Link>
                    ) : undefined
                  }
                />
              </Card>
            </li>
          ))}
        </ul>
      )}
    </AppShell>
  )
}
