import { apiClient } from '../../lib/apiClient'
import type { TicketDto } from './types'

export interface CreateTicketInput {
  title: string
  description: string
  categoryId: string
  files: File[]
}

export function getMyTickets(): Promise<TicketDto[]> {
  return apiClient.get<TicketDto[]>('/tickets')
}

export function createTicket(input: CreateTicketInput): Promise<TicketDto> {
  const formData = new FormData()
  formData.append('title', input.title)
  formData.append('description', input.description)
  formData.append('categoryId', input.categoryId)

  for (const file of input.files) {
    formData.append('files', file)
  }

  return apiClient.postForm<TicketDto>('/tickets', formData)
}
