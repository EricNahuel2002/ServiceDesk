interface ChatMessageBubbleProps {
  senderName: string
  content: string
  sentAtUtc: string
  isOwn: boolean
}

function formatTime(utcString: string): string {
  const date = new Date(utcString)
  return date.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })
}

export function ChatMessageBubble({ senderName, content, sentAtUtc, isOwn }: ChatMessageBubbleProps) {
  return (
    <div className={`flex flex-col ${isOwn ? 'items-end' : 'items-start'}`}>
      <span className="mb-0.5 text-[10px] font-medium text-gray-500">
        {isOwn ? 'Tú' : senderName}
      </span>
      <div
        className={`max-w-[80%] rounded-xl px-3 py-2 text-sm ${
          isOwn
            ? 'bg-emerald-500 text-white'
            : 'bg-gray-100 text-gray-900'
        }`}
      >
        <p className="whitespace-pre-wrap break-words">{content}</p>
      </div>
      <span className="mt-0.5 text-[10px] text-gray-400">{formatTime(sentAtUtc)}</span>
    </div>
  )
}
