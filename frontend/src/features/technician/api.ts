import { apiClient } from '../../lib/apiClient'
import type { TicketDto } from '../tickets/types'

export function getAssignedTickets(): Promise<TicketDto[]> {
  return apiClient.get<TicketDto[]>('/technician/tickets')
}

export function resolveTicket(id: string, resolutionNote: string): Promise<TicketDto> {
  return apiClient.patch<TicketDto>(`/technician/tickets/${id}/resolve`, { resolutionNote })
}
