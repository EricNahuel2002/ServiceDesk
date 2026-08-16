import { apiClient } from '../../lib/apiClient'
import type { TicketDto, TechnicianDto, UpdateTicketRequest } from '../tickets/types'

export function getAllTickets(): Promise<TicketDto[]> {
  return apiClient.get<TicketDto[]>('/admin/tickets')
}

export function getTicketById(id: string): Promise<TicketDto> {
  return apiClient.get<TicketDto>(`/admin/tickets/${id}`)
}

export function getTechnicians(): Promise<TechnicianDto[]> {
  return apiClient.get<TechnicianDto[]>('/admin/tickets/technicians')
}

export function updateTicket(id: string, data: UpdateTicketRequest): Promise<TicketDto> {
  return apiClient.patch<TicketDto>(`/admin/tickets/${id}`, data)
}
