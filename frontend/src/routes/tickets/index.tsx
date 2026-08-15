import { createFileRoute, Link } from '@tanstack/react-router'
import { Badge } from '../../components/common/Badge'
import { Card } from '../../components/common/Card'
import { AppShell } from '../../components/layout/AppShell'
import { useTickets } from '../../features/tickets/queries'
import { formatDate } from '../../utils/format'

export const Route = createFileRoute('/tickets/')({
  component: TicketsPage,
})

function TicketsPage() {
  const tickets = useTickets()

  return (
    <AppShell>
      <h1 className="mb-6 text-2xl font-bold text-gray-900">Mis tickets</h1>
      {tickets.isPending ? (
        <p className="text-gray-500">Cargando...</p>
      ) : tickets.isError ? (
        <p className="text-red-600">No se pudieron cargar los tickets.</p>
      ) : (
        <ul className="flex flex-col gap-3">
          {tickets.data?.map((ticket) => (
            <li key={ticket.id}>
              <Link
                to="/tickets/$ticketId"
                params={{ ticketId: ticket.id }}
                className="block"
              >
                <Card>
                  <div className="flex items-center justify-between gap-4">
                    <div>
                      <p className="font-medium text-gray-900">{ticket.title}</p>
                      <p className="text-sm text-gray-500">{ticket.categoryName}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge color="blue">{ticket.priorityName}</Badge>
                      <Badge>{ticket.statusName}</Badge>
                      <span className="text-sm text-gray-400">
                        {formatDate(ticket.createdAtUtc)}
                      </span>
                    </div>
                  </div>
                </Card>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </AppShell>
  )
}
