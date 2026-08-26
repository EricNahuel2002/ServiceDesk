import { apiClient } from '../../lib/apiClient'
import type { CreateTechnicianReportInput, SubmitTicketFeedbackRequest, TicketDto } from './types'

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

export function submitFeedback(ticketId: string, input: SubmitTicketFeedbackRequest): Promise<TicketDto> {
  return apiClient.post<TicketDto>(`/tickets/${ticketId}/feedback`, {
    wasSolved: input.wasSolved,
    rating: input.rating ?? null,
    comment: input.comment ?? null,
  })
}

export function createTechnicianReport(
  ticketId: string,
  input: CreateTechnicianReportInput,
): Promise<TicketDto> {
  const formData = new FormData()
  formData.append('reason', input.reason ?? '')

  for (const file of input.files) {
    formData.append('files', file)
  }

  return apiClient.postForm<TicketDto>(`/tickets/${ticketId}/technician-report`, formData)
}
