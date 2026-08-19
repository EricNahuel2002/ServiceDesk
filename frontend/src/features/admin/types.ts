export interface CreateCategoryRequest {
  name: string
}

export interface UpdateCategoryRequest {
  name: string
  isActive: boolean
}

export interface CreateStatusRequest {
  name: string
  sortOrder: number
  isClosed: boolean
}

export interface UpdateStatusRequest {
  name: string
  sortOrder: number
  isClosed: boolean
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
}
