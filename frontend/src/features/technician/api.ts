import { apiClient } from '../../lib/apiClient'
import type { TicketDto } from '../tickets/types'

export function getAssignedTickets(): Promise<TicketDto[]> {
  return apiClient.get<TicketDto[]>('/technician/tickets')
}

export function startWork(id: string): Promise<TicketDto> {
  return apiClient.patch<TicketDto>(`/technician/tickets/${id}/start-work`)
}

export function resolveTicket(id: string, resolutionNote: string): Promise<TicketDto> {
  return apiClient.patch<TicketDto>(`/technician/tickets/${id}/resolve`, { resolutionNote })
}
