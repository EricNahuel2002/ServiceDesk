import { apiClient } from '../../lib/apiClient'
import type { ChatMessageDto } from './types'

export function getChatHistory(ticketId: string): Promise<ChatMessageDto[]> {
  return apiClient.get<ChatMessageDto[]>(`/tickets/${ticketId}/chat`)
}
