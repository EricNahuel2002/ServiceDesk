import { useState } from 'react'
import { useAuth } from '../../hooks/useAuth'
import { useChatWidget } from '../../features/chat/useChatWidget'
import { ChatWidgetTicketList } from './ChatWidgetTicketList'
import { ChatWidgetPanel } from './ChatWidgetPanel'
import { ChatIcon } from '../common/ChatIcon'

interface TicketItem {
  id: string
  title: string
}

interface ChatWidgetProps {
  tickets: TicketItem[]
}

export function ChatWidget({ tickets }: ChatWidgetProps) {
  const { isAuthenticated } = useAuth()
  const [isOpen, setIsOpen] = useState(false)
  const {
    unreadCounts,
    lastMessages,
    activeChatTicketId,
    totalUnread,
    openChat,
    closeChat,
  } = useChatWidget(tickets)

  if (!isAuthenticated || tickets.length === 0) return null

  const activeTicket = activeChatTicketId
    ? tickets.find((t) => t.id === activeChatTicketId)
    : null

  function handleToggle() {
    if (isOpen) {
      closeChat()
    }
    setIsOpen(!isOpen)
  }

  function handleSelectTicket(ticketId: string) {
    openChat(ticketId)
  }

  function handleBack() {
    closeChat()
  }

  return (
    <div className="fixed bottom-4 right-4 z-50">
      {isOpen && (
        <div className="mb-3 flex w-80 flex-col overflow-hidden rounded-xl border border-gray-200 bg-white shadow-2xl sm:w-96"
          style={{ height: '480px' }}
        >
          <div className="flex items-center justify-between border-b border-gray-200 bg-emerald-500 px-4 py-3">
            <h3 className="text-sm font-semibold text-white">Chats</h3>
            <button
              type="button"
              onClick={handleToggle}
              className="text-emerald-100 hover:text-white"
            >
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" className="h-4 w-4">
                <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
              </svg>
            </button>
          </div>

          <div className="flex-1 overflow-y-auto">
            {activeTicket ? (
              <ChatWidgetPanel
                ticketId={activeTicket.id}
                ticketTitle={activeTicket.title}
                onBack={handleBack}
              />
            ) : (
              <ChatWidgetTicketList
                tickets={tickets}
                lastMessages={lastMessages}
                unreadCounts={unreadCounts}
                onSelectTicket={handleSelectTicket}
              />
            )}
          </div>
        </div>
      )}

      <div className="flex justify-end">
        <button
          type="button"
          onClick={handleToggle}
          className="relative flex h-14 w-14 items-center justify-center rounded-full bg-emerald-500 text-white shadow-lg transition-transform hover:scale-105 hover:bg-emerald-600"
        >
          <ChatIcon className="h-6 w-6" />
          {totalUnread > 0 && !isOpen && (
            <span className="absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-bold text-white">
              {totalUnread > 99 ? '99+' : totalUnread}
            </span>
          )}
        </button>
      </div>
    </div>
  )
}
