interface ChatTypingIndicatorProps {
  names: string[]
}

export function ChatTypingIndicator({ names }: ChatTypingIndicatorProps) {
  if (names.length === 0) return null

  const text =
    names.length === 1
      ? `${names[0]} está escribiendo`
      : `${names.join(' y ')} están escribiendo`

  return (
    <div className="flex items-center gap-1.5 px-1 py-1">
      <div className="flex gap-0.5">
        <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-gray-400 [animation-delay:-0.3s]" />
        <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-gray-400 [animation-delay:-0.15s]" />
        <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-gray-400" />
      </div>
      <span className="text-xs text-gray-500">{text}...</span>
    </div>
  )
}
