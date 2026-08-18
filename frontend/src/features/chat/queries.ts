import { useQuery } from '@tanstack/react-query'
import { getChatHistory } from './api'

export function useChatHistory(ticketId: string) {
  return useQuery({
    queryKey: ['chat', ticketId],
    queryFn: () => getChatHistory(ticketId),
    enabled: Boolean(ticketId),
  })
}
