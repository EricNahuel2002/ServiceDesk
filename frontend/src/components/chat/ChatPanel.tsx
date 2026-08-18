import { useCallback, useEffect, useRef, useState } from 'react'
import { ChatMessageBubble } from './ChatMessageBubble'
import { ChatTypingIndicator } from './ChatTypingIndicator'
import { useChat } from '../../features/chat/useChat'
import { useChatHistory } from '../../features/chat/queries'
import { chatConnection } from '../../features/chat/signalr'

interface ChatPanelProps {
  ticketId: string
}

export function ChatPanel({ ticketId }: ChatPanelProps) {
  const { messages: realtimeMessages, typingUsers, sendMessage, sendTyping, currentUserId } = useChat(ticketId)
  const history = useChatHistory(ticketId)
  const [input, setInput] = useState('')
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const typingTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const allMessages = (() => {
    const historyMessages = history.data ?? []
    const seen = new Set(historyMessages.map((m) => m.id))
    const extra = realtimeMessages.filter((m) => !seen.has(m.id))
    return [...historyMessages, ...extra].sort(
      (a, b) => new Date(a.sentAtUtc).getTime() - new Date(b.sentAtUtc).getTime(),
    )
  })()

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [allMessages.length])

  useEffect(() => {
    const unsubReconnected = chatConnection.onReconnected(() => {
      void history.refetch()
    })
    return unsubReconnected
  }, [history])

  const handleTyping = useCallback(() => {
    sendTyping()
  }, [sendTyping])

  const handleInputChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setInput(e.target.value)
      handleTyping()
    },
    [handleTyping],
  )

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const trimmed = input.trim()
    if (!trimmed) return
    setInput('')
    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current)
      typingTimeoutRef.current = null
    }
    await sendMessage(trimmed)
  }

  return (
    <div className="flex flex-col rounded-lg border border-gray-200 bg-white" style={{ height: '400px' }}>
      <div className="border-b border-gray-200 px-4 py-3">
        <h3 className="text-sm font-semibold text-gray-900">Chat</h3>
      </div>

      <div className="flex-1 overflow-y-auto px-4 py-3">
        {history.isPending ? (
          <p className="py-4 text-center text-sm text-gray-400">Cargando mensajes...</p>
        ) : allMessages.length === 0 ? (
          <p className="py-4 text-center text-sm text-gray-400">
            No hay mensajes aún. ¡Empezá la conversación!
          </p>
        ) : (
          <div className="flex flex-col gap-3">
            {allMessages.map((msg) => (
              <ChatMessageBubble
                key={msg.id}
                senderName={`${msg.senderFirstName} ${msg.senderLastName}`}
                content={msg.content}
                sentAtUtc={msg.sentAtUtc}
                isOwn={msg.senderId === currentUserId}
              />
            ))}
            <ChatTypingIndicator names={typingUsers.map((u) => u.firstName)} />
            <div ref={messagesEndRef} />
          </div>
        )}
      </div>

      <form onSubmit={handleSubmit} className="flex gap-2 border-t border-gray-200 px-4 py-3">
        <input
          type="text"
          value={input}
          onChange={handleInputChange}
          placeholder="Escribí un mensaje..."
          className="flex-1 rounded-md border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder:text-gray-400 focus:border-emerald-500 focus:outline-none"
        />
        <button
          type="submit"
          disabled={!input.trim()}
          className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-600 disabled:opacity-50"
        >
          Enviar
        </button>
      </form>
    </div>
  )
}
