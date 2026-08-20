import { useCallback, useEffect, useRef, useState } from 'react'
import { useAuth } from '../../hooks/useAuth'
import { chatConnection } from './signalr'
import type { ChatMessageDto } from './types'

export function useChat(ticketId: string) {
  const { user, isAuthenticated } = useAuth()
  const [messages, setMessages] = useState<ChatMessageDto[]>([])
  const [typingUsers, setTypingUsers] = useState<Map<string, { firstName: string; lastName: string }>>(new Map())
  const typingTimeoutRef = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map())

  useEffect(() => {
    if (!isAuthenticated || !user || !ticketId) return

    let mounted = true

    async function connect() {
      try {
        await chatConnection.start()
      } catch {
        // Connection will retry automatically
      }
    }

    void connect()

    const unsubMessage = chatConnection.onReceiveMessage(ticketId, (message) => {
      if (mounted) {
        setMessages((prev) => {
          if (prev.some((m) => m.id === message.id)) return prev
          return [...prev, message]
        })
      }
    })

    const unsubTyping = chatConnection.onUserTyping(ticketId, (data) => {
      if (data.userId === user.id) return
      if (!mounted) return

      setTypingUsers((prev) => {
        const next = new Map(prev)
        next.set(data.userId, { firstName: data.firstName, lastName: data.lastName })
        return next
      })

      const existing = typingTimeoutRef.current.get(data.userId)
      if (existing) clearTimeout(existing)

      const timeout = setTimeout(() => {
        if (mounted) {
          setTypingUsers((prev) => {
            const next = new Map(prev)
            next.delete(data.userId)
            return next
          })
        }
        typingTimeoutRef.current.delete(data.userId)
      }, 3000)

      typingTimeoutRef.current.set(data.userId, timeout)
    })

    return () => {
      mounted = false
      unsubMessage()
      unsubTyping()
      typingTimeoutRef.current.forEach((t) => clearTimeout(t))
      typingTimeoutRef.current.clear()
    }
  }, [ticketId, user, isAuthenticated])

  const sendMessage = useCallback(
    async (content: string) => {
      if (!ticketId || !content.trim()) return
      await chatConnection.sendMessage(ticketId, content)
    },
    [ticketId],
  )

  const sendTyping = useCallback(() => {
    if (!ticketId) return
    void chatConnection.sendTyping(ticketId)
  }, [ticketId])

  return {
    messages,
    typingUsers: Array.from(typingUsers.values()),
    sendMessage,
    sendTyping,
    currentUserId: user?.id ?? '',
  }
}
