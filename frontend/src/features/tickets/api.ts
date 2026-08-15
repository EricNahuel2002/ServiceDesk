import { apiClient } from '../../lib/apiClient'
import type { TicketDto } from './types'

export function getMyTickets(): Promise<TicketDto[]> {
  return apiClient.get<TicketDto[]>('/tickets')
}
