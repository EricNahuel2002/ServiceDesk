import { createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import { Card } from '../../../components/common/Card'
import { Badge } from '../../../components/common/Badge'
import { Select } from '../../../components/common/Select'
import { AdminAppShell } from '../../../components/layout/AdminAppShell'
import { requireAdmin } from '../../../features/admin/auth'
import {
  useAuditTechnicians,
  useAuditTechnicianTickets,
  useAuditTicketHistory,
  useAuditTicketChat,
} from '../../../features/admin/queries'
import { formatTime, formatDate } from '../../../utils/format'
import { getStatusBadgeColor } from '../../../utils/status'
import type { TechnicianDto } from '../../../features/tickets/types'
import type { TicketDto } from '../../../features/tickets/types'
import type { TicketAuditEventDto } from '../../../features/admin/types'
import type { ChatMessageDto } from '../../../features/chat/types'

export const Route = createFileRoute('/admin/audits/')({
  beforeLoad: () => requireAdmin(),
  component: AdminAuditsPage,
})

function AdminAuditsPage() {
  const [selectedTechnicianId, setSelectedTechnicianId] = useState('')
  const [selectedTicketId, setSelectedTicketId] = useState('')

  const technicians = useAuditTechnicians()

  function handleTechnicianChange(e: React.ChangeEvent<HTMLSelectElement>) {
    setSelectedTechnicianId(e.target.value)
    setSelectedTicketId('')
  }

  return (
    <AdminAppShell>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Auditorías</h1>
      </div>

      <div className="mb-6">
        <Select
          label="Técnico"
          value={selectedTechnicianId}
          onChange={handleTechnicianChange}
        >
          <option value="">Seleccionar técnico...</option>
          {technicians.data?.map((tech: TechnicianDto) => (
            <option key={tech.id} value={tech.id}>
              {tech.firstName} {tech.lastName}
            </option>
          ))}
        </Select>
      </div>

      {selectedTechnicianId && (
        <div className="grid gap-6 lg:grid-cols-3">
          <div className="lg:col-span-1">
            <TechnicianTicketsPanel
              technicianId={selectedTechnicianId}
              selectedTicketId={selectedTicketId}
              onSelectTicket={setSelectedTicketId}
            />
          </div>

          {selectedTicketId && (
            <div className="lg:col-span-2">
              <TicketAuditGrid ticketId={selectedTicketId} />
            </div>
          )}
        </div>
      )}
    </AdminAppShell>
  )
}

function TechnicianTicketsPanel({
  technicianId,
  selectedTicketId,
  onSelectTicket,
}: {
  technicianId: string
  selectedTicketId: string
  onSelectTicket: (id: string) => void
}) {
  const tickets = useAuditTechnicianTickets(technicianId)

  if (tickets.isPending) {
    return <p className="text-gray-500">Cargando tickets...</p>
  }

  const ticketList = tickets.data ?? []

  if (ticketList.length === 0) {
    return (
      <Card>
        <p className="text-sm text-gray-500">Este técnico no tiene tickets asignados.</p>
      </Card>
    )
  }

  return (
    <Card>
      <h2 className="mb-3 text-sm font-semibold text-gray-900">
        Tickets asignados ({ticketList.length})
      </h2>
      <ul className="flex flex-col gap-2">
        {ticketList.map((ticket: TicketDto) => (
          <li key={ticket.id}>
            <button
              type="button"
              onClick={() => onSelectTicket(ticket.id)}
              className={`w-full rounded-md border px-4 py-3 text-left transition-colors ${
                selectedTicketId === ticket.id
                  ? 'border-[#0F52BA] bg-blue-50'
                  : 'border-gray-200 hover:bg-gray-50'
              }`}
            >
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium text-gray-900">
                  {ticket.title}
                </span>
                <div className="flex items-center gap-2">
                  <Badge color={getStatusBadgeColor(ticket.statusName)}>
                    {ticket.statusName}
                  </Badge>
                  <span className="text-xs text-gray-400">
                    {formatDate(ticket.createdAtUtc)}
                  </span>
                </div>
              </div>
            </button>
          </li>
        ))}
      </ul>
    </Card>
  )
}

function TicketAuditGrid({ ticketId }: { ticketId: string }) {
  const history = useAuditTicketHistory(ticketId)
  const chat = useAuditTicketChat(ticketId)

  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <Card>
        <h2 className="mb-4 text-lg font-semibold text-gray-900">Historial de eventos</h2>
        {history.isPending ? (
          <p className="text-gray-500">Cargando historial...</p>
        ) : (history.data ?? []).length === 0 ? (
          <p className="text-sm text-gray-400">No hay eventos registrados para este ticket.</p>
        ) : (
          <Timeline events={history.data ?? []} />
        )}
      </Card>

      <Card>
        <h2 className="mb-4 text-lg font-semibold text-gray-900">Chat con el cliente</h2>
        {chat.isPending ? (
          <p className="text-gray-500">Cargando chat...</p>
        ) : (chat.data ?? []).length === 0 ? (
          <p className="text-sm text-gray-400">No hay mensajes de chat para este ticket.</p>
        ) : (
          <ChatReadOnly messages={chat.data ?? []} />
        )}
      </Card>
    </div>
  )
}

function Timeline({ events }: { events: TicketAuditEventDto[] }) {
  return (
    <div className="relative ml-3 border-l-2 border-gray-200 pl-6">
      {events.map((event, index) => (
        <div key={index} className="relative mb-6 last:mb-0">
          <div className="absolute -left-[31px] top-1 h-3 w-3 rounded-full border-2 border-[#0F52BA] bg-white" />
          <div className="flex flex-col gap-1">
            <span className="text-xs font-semibold text-[#0F52BA]">
              {formatTime(event.occurredAtUtc)}
            </span>
            <span className="text-sm font-medium text-gray-900">
              {event.description}
            </span>
            {event.details && (
              <p className="text-xs text-gray-500 italic">
                {event.details}
              </p>
            )}
            {event.actorName && (
              <span className="text-xs text-gray-400">
                por {event.actorName}
              </span>
            )}
          </div>
        </div>
      ))}
    </div>
  )
}

function ChatReadOnly({ messages }: { messages: ChatMessageDto[] }) {
  const sorted = [...messages].sort(
    (a, b) => new Date(a.sentAtUtc).getTime() - new Date(b.sentAtUtc).getTime(),
  )

  return (
    <div className="flex flex-col gap-3 max-h-[500px] overflow-y-auto">
      {sorted.map((msg) => (
        <div key={msg.id} className="flex flex-col items-start">
          <span className="mb-0.5 text-[10px] font-medium text-gray-500">
            {msg.senderFirstName} {msg.senderLastName}
          </span>
          <div className="max-w-[80%] rounded-xl bg-gray-100 px-3 py-2 text-sm text-gray-900">
            <p className="whitespace-pre-wrap break-words">{msg.content}</p>
          </div>
          <span className="mt-0.5 text-[10px] text-gray-400">
            {formatTime(msg.sentAtUtc)}
          </span>
        </div>
      ))}
    </div>
  )
}
