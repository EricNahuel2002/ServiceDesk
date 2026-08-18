import { useCallback, useEffect, useRef, useState } from 'react'
import { ChatMessageBubble } from './ChatMessageBubble'
import { ChatTypingIndicator } from './ChatTypingIndicator'
import { useChat } from '../../features/chat/useChat'
import { useChatHistory } from '../../features/chat/queries'
import { chatConnection } from '../../features/chat/signalr'

interface ChatWidgetPanelProps {
  ticketId: string
  ticketTitle: string
  onBack: () => void
}

export function ChatWidgetPanel({ ticketId, ticketTitle, onBack }: ChatWidgetPanelProps) {
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
    <div className="flex h-full flex-col">
      <div className="flex items-center gap-2 border-b border-gray-200 px-3 py-2">
        <button
          type="button"
          onClick={onBack}
          className="text-gray-500 hover:text-gray-700"
        >
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" className="h-4 w-4">
            <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
          </svg>
        </button>
        <span className="truncate text-sm font-semibold text-gray-900">{ticketTitle}</span>
      </div>

      <div className="flex-1 overflow-y-auto px-3 py-2">
        {history.isPending ? (
          <p className="py-4 text-center text-xs text-gray-400">Cargando mensajes...</p>
        ) : allMessages.length === 0 ? (
          <p className="py-4 text-center text-xs text-gray-400">No hay mensajes aún. ¡Empezá la conversación!</p>
        ) : (
          <div className="flex flex-col gap-2">
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

      <form onSubmit={handleSubmit} className="flex gap-2 border-t border-gray-200 px-3 py-2">
        <input
          type="text"
          value={input}
          onChange={handleInputChange}
          placeholder="Escribí un mensaje..."
          className="flex-1 rounded-lg border border-gray-300 px-3 py-1.5 text-sm text-gray-900 placeholder:text-gray-400 focus:border-emerald-500 focus:outline-none"
        />
        <button
          type="submit"
          disabled={!input.trim()}
          className="rounded-lg bg-emerald-500 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-600 disabled:opacity-50"
        >
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" className="h-4 w-4">
            <path strokeLinecap="round" strokeLinejoin="round" d="M6 12 3.269 3.125A59.769 59.769 0 0 1 21.485 12 59.768 59.768 0 0 1 3.27 20.875L5.999 12Zm0 0h7.5" />
          </svg>
        </button>
      </form>
    </div>
  )
}
