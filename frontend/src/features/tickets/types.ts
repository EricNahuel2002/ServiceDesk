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
  priority: number | null
  statusId: string
  statusName: string
  createdById: string
  assignedToId: string | null
  assignedToFirstName: string | null
  assignedToLastName: string | null
  assignedToEmail: string | null
  createdAtUtc: string
  updatedAtUtc: string | null
  assignedAtUtc: string | null
  responseDeadlineAtUtc: string
  startedWorkAtUtc: string | null
  resolvedAtUtc: string | null
  slaLimitHours: number
  delayMinutes: number
  effectiveSlaLimitHours: number
  slaPercentageElapsed: number
  isOverdue: boolean
  hasPendingFeedback: boolean
  canReportTechnician: boolean
  attachments: TicketAttachmentDto[]
}

export interface CreateTicketRequest {
  title: string
  description: string
  categoryId: string
}

export interface UpdateTicketRequest {
  assignedToId?: string | null
  priority?: number | null
}

export interface AssignTicketRequest {
  assignedToId: string
}

export interface SubmitTicketFeedbackRequest {
  wasSolved: boolean
  rating?: number | null
  comment?: string | null
}

export interface CreateTechnicianReportInput {
  reason?: string | null
  files: File[]
}

export interface TechnicianDto {
  id: string
  firstName: string
  lastName: string
  email: string
}
