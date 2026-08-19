export interface TicketAttachmentDto {
  id: string
  fileName: string
  contentType: string
  sizeInBytes: number
  blobName: string
}

export interface TicketDto {
  id: string
  title: string
  description: string
  companyId: string
  categoryId: string
  categoryName: string
  priority: number
  statusId: string
  statusName: string
  createdById: string
  assignedToId: string | null
  assignedToFirstName: string | null
  assignedToLastName: string | null
  assignedToEmail: string | null
  createdAtUtc: string
  updatedAtUtc: string | null
  responseDeadlineAtUtc: string
  startedWorkAtUtc: string | null
  slaLimitHours: number
  slaPercentageElapsed: number
  isOverdue: boolean
  attachments: TicketAttachmentDto[]
}

export interface CreateTicketRequest {
  title: string
  description: string
  categoryId: string
}

export interface UpdateTicketRequest {
  assignedToId: string
  priority: number
  statusId: string
}

export interface TechnicianDto {
  id: string
  firstName: string
  lastName: string
  email: string
}
