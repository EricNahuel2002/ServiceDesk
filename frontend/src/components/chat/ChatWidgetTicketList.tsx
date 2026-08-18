import type { ChatMessageDto } from '../../features/chat/types'

interface TicketItem {
  id: string
  title: string
}

interface ChatWidgetTicketListProps {
  tickets: TicketItem[]
  lastMessages: { [ticketId: string]: ChatMessageDto }
  unreadCounts: { [ticketId: string]: number }
  onSelectTicket: (ticketId: string) => void
}

function formatRelativeTime(utcString: string): string {
  const now = Date.now()
  const then = new Date(utcString).getTime()
  const diffMs = now - then
  const diffMin = Math.floor(diffMs / 60_000)

  if (diffMin < 1) return 'ahora'
  if (diffMin < 60) return `${diffMin}m`
  const diffHrs = Math.floor(diffMin / 60)
  if (diffHrs < 24) return `${diffHrs}h`
  const diffDays = Math.floor(diffHrs / 24)
  return `${diffDays}d`
}

export function ChatWidgetTicketList({
  tickets,
  lastMessages,
  unreadCounts,
  onSelectTicket,
}: ChatWidgetTicketListProps) {
  const ticketsWithActivity = tickets
    .filter((t) => lastMessages[t.id])
    .sort((a, b) => {
      const dateA = new Date(lastMessages[a.id].sentAtUtc).getTime()
      const dateB = new Date(lastMessages[b.id].sentAtUtc).getTime()
      return dateB - dateA
    })

  const ticketsWithoutActivity = tickets.filter((t) => !lastMessages[t.id])

  if (tickets.length === 0) {
    return (
      <p className="px-4 py-6 text-center text-sm text-gray-400">
        No tenés tickets disponibles para chat.
      </p>
    )
  }

  return (
    <div className="flex flex-col">
      {ticketsWithActivity.length > 0 && (
        <div>
          {ticketsWithActivity.map((ticket) => {
            const lastMsg = lastMessages[ticket.id]
            const unread = unreadCounts[ticket.id] ?? 0
            return (
              <button
                key={ticket.id}
                type="button"
                onClick={() => onSelectTicket(ticket.id)}
                className="flex w-full items-center gap-3 border-b border-gray-100 px-4 py-3 text-left hover:bg-gray-50"
              >
                <div className="min-w-0 flex-1">
                  <div className="flex items-center justify-between">
                    <span className="truncate text-sm font-medium text-gray-900">
                      {ticket.title}
                    </span>
                    <span className="ml-2 shrink-0 text-[10px] text-gray-400">
                      {formatRelativeTime(lastMsg.sentAtUtc)}
                    </span>
                  </div>
                  <p className="mt-0.5 truncate text-xs text-gray-500">
                    {lastMsg.senderFirstName}: {lastMsg.content}
                  </p>
                </div>
                {unread > 0 && (
                  <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-emerald-500 px-1.5 text-[10px] font-bold text-white">
                    {unread > 99 ? '99+' : unread}
                  </span>
                )}
              </button>
            )
          })}
        </div>
      )}

      {ticketsWithoutActivity.length > 0 && (
        <div>
          <div className="px-4 py-2">
            <span className="text-[10px] font-semibold uppercase tracking-wide text-gray-400">
              Sin mensajes
            </span>
          </div>
          {ticketsWithoutActivity.map((ticket) => (
            <button
              key={ticket.id}
              type="button"
              onClick={() => onSelectTicket(ticket.id)}
              className="flex w-full items-center gap-3 border-b border-gray-100 px-4 py-3 text-left hover:bg-gray-50"
            >
              <span className="truncate text-sm text-gray-600">{ticket.title}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
