import { createFileRoute, Link } from '@tanstack/react-router'
import { Badge } from '../../components/common/Badge'
import { Card } from '../../components/common/Card'
import { AppShell } from '../../components/layout/AppShell'
import { useTickets } from '../../features/tickets/queries'
import { formatDate } from '../../utils/format'

export const Route = createFileRoute('/tickets/$ticketId')({
  component: TicketDetailPage,
})

function TicketDetailPage() {
  const { ticketId } = Route.useParams()
  const tickets = useTickets()
  const ticket = tickets.data?.find((item) => item.id === ticketId)

  return (
    <AppShell>
      <Link to="/tickets" className="text-sm text-blue-600 hover:underline">
        Volver a mis tickets
      </Link>
      {ticket ? (
        <div className="mt-4 flex flex-col gap-4">
          <h1 className="text-2xl font-bold text-gray-900">{ticket.title}</h1>
          <div className="flex gap-2">
            <Badge color="blue">{ticket.priorityName}</Badge>
            <Badge>{ticket.statusName}</Badge>
          </div>
          <Card>
            <p className="whitespace-pre-wrap text-gray-700">{ticket.description}</p>
          </Card>
          <p className="text-sm text-gray-500">
            Creado el {formatDate(ticket.createdAtUtc)} · {ticket.categoryName}
          </p>
        </div>
      ) : tickets.isPending ? (
        <p className="mt-4 text-gray-500">Cargando...</p>
      ) : (
        <p className="mt-4 text-red-600">Ticket no encontrado.</p>
      )}
    </AppShell>
  )
}
