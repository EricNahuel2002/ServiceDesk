import { createRootRouteWithContext, Outlet } from '@tanstack/react-router'
import type { QueryClient } from '@tanstack/react-query'
import { useQuery } from '@tanstack/react-query'
import { TanStackRouterDevtools } from '@tanstack/router-devtools'
import { AuthProvider } from '../features/auth/AuthProvider'
import { ChatWidget } from '../components/chat/ChatWidget'
import { ChatWidgetContextProvider } from '../features/chat/ChatWidgetContext'
import { useAuth } from '../hooks/useAuth'
import { getMyTickets } from '../features/tickets/api'
import { getAssignedTickets } from '../features/technician/api'
import type { TicketDto } from '../features/tickets/types'

interface RouterContext {
  queryClient: QueryClient
}

export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootComponent,
})

function RootComponent() {
  return (
    <AuthProvider>
      <ChatWidgetContextProvider>
        <ChatWidgetWrapper />
        <Outlet />
      </ChatWidgetContextProvider>
      {import.meta.env.DEV ? <TanStackRouterDevtools /> : null}
    </AuthProvider>
  )
}

function ChatWidgetWrapper() {
  const { user, isAuthenticated } = useAuth()

  const isChatEnabled = Boolean(
    isAuthenticated &&
      user &&
      (user.role === 'Tecnico' || user.role === 'Cliente'),
  )

  const ticketsQuery = useQuery<TicketDto[]>({
    queryKey: ['chat-widget-tickets'],
    queryFn: user?.role === 'Tecnico' ? getAssignedTickets : getMyTickets,
    enabled: isChatEnabled,
    staleTime: 30_000,
  })

  if (!isChatEnabled) return null

  const tickets = (ticketsQuery.data ?? [])
    .filter((t) => t.assignedToId)
    .map((t) => ({ id: t.id, title: t.title }))

  return <ChatWidget tickets={tickets} />
}
