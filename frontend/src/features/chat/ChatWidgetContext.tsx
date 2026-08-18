import { createContext, useCallback, useContext, useRef } from 'react'

type OpenChatFn = (ticketId: string) => void

interface ChatWidgetContextValue {
  openChat: (ticketId: string) => void
  registerOpenChat: (fn: OpenChatFn) => () => void
}

const ChatWidgetContext = createContext<ChatWidgetContextValue>({
  openChat: () => {},
  registerOpenChat: () => () => {},
})

export function useOpenChat() {
  return useContext(ChatWidgetContext).openChat
}

export function useRegisterOpenChat() {
  return useContext(ChatWidgetContext).registerOpenChat
}

export function ChatWidgetContextProvider({ children }: { children: React.ReactNode }) {
  const openChatRef = useRef<OpenChatFn | null>(null)

  const registerOpenChat = useCallback((fn: OpenChatFn) => {
    openChatRef.current = fn
    return () => { openChatRef.current = null }
  }, [])

  const openChat = useCallback((ticketId: string) => {
    openChatRef.current?.(ticketId)
  }, [])

  return (
    <ChatWidgetContext.Provider value={{ openChat, registerOpenChat }}>
      {children}
    </ChatWidgetContext.Provider>
  )
}
