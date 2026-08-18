import { useCallback, useEffect, useState } from 'react'
import type { QueryClient } from '@tanstack/react-query'
import { useAuth } from '../../hooks/useAuth'
import { chatConnection } from './signalr'
import type { ChatMessageDto } from './types'

interface TicketInfo {
  id: string
  title: string
}

interface UnreadCount {
  [ticketId: string]: number
}

interface LastMessage {
  [ticketId: string]: ChatMessageDto
}

export function useChatWidget(tickets: TicketInfo[], queryClient: QueryClient) {
  const { user, isAuthenticated } = useAuth()
  const [unreadCounts, setUnreadCounts] = useState<UnreadCount>({})
  const [lastMessages, setLastMessages] = useState<LastMessage>({})
  const [onlineUserIds, setOnlineUserIds] = useState<Set<string>>(new Set())
  const [activeChatTicketId, setActiveChatTicketId] = useState<string | null>(null)

  useEffect(() => {
    if (!isAuthenticated || !user || tickets.length === 0) return

    let mounted = true

    async function connect() {
      try {
        await chatConnection.start()
        if (!mounted) return

        for (const ticket of tickets) {
          if (mounted) {
            await chatConnection.joinTicket(ticket.id)
          }
        }
      } catch {
        // Connection will retry automatically
      }
    }

    void connect()

    const unsubs: Array<() => void> = []

    for (const ticket of tickets) {
      const unsub = chatConnection.onReceiveMessage(ticket.id, (message) => {
        if (!mounted) return

        if (message.senderId !== user.id) {
          setUnreadCounts((prev) => ({
            ...prev,
            [ticket.id]: (prev[ticket.id] ?? 0) + 1,
          }))
        }

        setLastMessages((prev) => ({
          ...prev,
          [ticket.id]: message,
        }))

        queryClient.invalidateQueries({ queryKey: ['chat', ticket.id] })
      })
      unsubs.push(unsub)
    }

    const unsubConnected = chatConnection.onUserConnected((data) => {
      if (mounted) {
        setOnlineUserIds((prev) => new Set(prev).add(data.userId))
      }
    })

    const unsubDisconnected = chatConnection.onUserDisconnected((data) => {
      if (mounted) {
        setOnlineUserIds((prev) => {
          const next = new Set(prev)
          next.delete(data.userId)
          return next
        })
      }
    })

    unsubs.push(unsubConnected, unsubDisconnected)

    return () => {
      mounted = false
      unsubs.forEach((u) => u())
    }
  }, [tickets, user, isAuthenticated, queryClient])

  const openChat = useCallback((ticketId: string) => {
    setActiveChatTicketId(ticketId)
    setUnreadCounts((prev) => ({
      ...prev,
      [ticketId]: 0,
    }))
  }, [])

  const closeChat = useCallback(() => {
    setActiveChatTicketId(null)
  }, [])

  const totalUnread = Object.values(unreadCounts).reduce((sum, count) => sum + count, 0)

  return {
    unreadCounts,
    lastMessages,
    onlineUserIds,
    activeChatTicketId,
    totalUnread,
    openChat,
    closeChat,
  }
}
