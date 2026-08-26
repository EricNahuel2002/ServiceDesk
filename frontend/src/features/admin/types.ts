export interface CreateCategoryRequest {
  name: string
}

export interface UpdateCategoryRequest {
  name: string
  isActive: boolean
}

export interface CreateUserRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  companyId: string
  role: string
}

export interface UserListItemDto {
  id: string
  firstName: string
  lastName: string
  email: string
  role: string
  isActive: boolean
  createdAtUtc: string
}

export interface AdminMetricsDto {
  totalTickets: number
  openTickets: number
  inProgressTickets: number
  resolvedTickets: number
  overdueTickets: number
  averageResolutionHours: number
  averageStartHours: number
  slaCompliancePercentage: number
  byPriority: PriorityMetricDto[]
  dailyTrend: DailyMetricDto[]
  byTechnician: TechnicianMetricDto[]
}

export interface PriorityMetricDto {
  priority: number
  count: number
  overdueCount: number
}

export interface DailyMetricDto {
  date: string
  created: number
  resolved: number
}

export interface TechnicianMetricDto {
  userId: string
  firstName: string
  lastName: string
  assignedCount: number
  resolvedCount: number
  averageResolutionHours: number
  averageStartHours: number
}

export interface SlaConfigurationDto {
  priority: number
  responseTimeHours: number
}

export interface UpdateSlaConfigurationRequest {
  priority: number
  responseTimeHours: number
}

export interface BusinessHoursDto {
  businessHoursJson: string
  timeZoneId: string
  useBusinessHours: boolean
  maxAssignmentToStartMinutes: number
}

export interface UpdateBusinessHoursRequest {
  businessHoursJson: string
  timeZoneId: string
  useBusinessHours: boolean
  maxAssignmentToStartMinutes: number
}

export interface DaySchedule {
  enabled: boolean
  start: string | null
  end: string | null
}

export interface TicketAuditEventDto {
  occurredAtUtc: string
  action: string
  description: string
  details: string | null
  actorName: string
}
