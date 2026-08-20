import { apiClient } from '../../lib/apiClient'
import type { TicketDto, TechnicianDto, UpdateTicketRequest, AssignTicketRequest } from '../tickets/types'
import type {
  CreateCategoryRequest,
  UpdateCategoryRequest,
  CreateUserRequest,
} from './types'
import type { CategoryDto } from '../catalog/types'
import type { UserListItemDto } from './types'
import type { AdminMetricsDto } from './types'
import type {
  SlaConfigurationDto,
  UpdateSlaConfigurationRequest,
  BusinessHoursDto,
  UpdateBusinessHoursRequest,
} from './types'

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

export function assignTicket(id: string, data: AssignTicketRequest): Promise<TicketDto> {
  return apiClient.patch<TicketDto>(`/admin/tickets/${id}/assign`, data)
}

export function getAllCategories(): Promise<CategoryDto[]> {
  return apiClient.get<CategoryDto[]>('/admin/catalog/categories')
}

export function createCategory(data: CreateCategoryRequest): Promise<CategoryDto> {
  return apiClient.post<CategoryDto>('/admin/catalog/categories', data)
}

export function updateCategory(id: string, data: UpdateCategoryRequest): Promise<CategoryDto> {
  return apiClient.put<CategoryDto>(`/admin/catalog/categories/${id}`, data)
}

export function getAllUsers(): Promise<UserListItemDto[]> {
  return apiClient.get<UserListItemDto[]>('/users')
}

export function createUser(data: CreateUserRequest): Promise<unknown> {
  return apiClient.post('/users', data)
}

export function getMetrics(params: {
  from?: string
  to?: string
  technicianId?: string
  period?: string
}): Promise<AdminMetricsDto> {
  const searchParams = new URLSearchParams()
  if (params.from) searchParams.set('from', params.from)
  if (params.to) searchParams.set('to', params.to)
  if (params.technicianId) searchParams.set('technicianId', params.technicianId)
  if (params.period) searchParams.set('period', params.period)
  const query = searchParams.toString()
  return apiClient.get<AdminMetricsDto>(`/admin/metrics${query ? `?${query}` : ''}`)
}

export function getSlaConfigurations(): Promise<SlaConfigurationDto[]> {
  return apiClient.get<SlaConfigurationDto[]>('/admin/sla/configurations')
}

export function updateSlaConfiguration(data: UpdateSlaConfigurationRequest): Promise<SlaConfigurationDto> {
  return apiClient.put<SlaConfigurationDto>('/admin/sla/configurations', data)
}

export function getBusinessHours(): Promise<BusinessHoursDto> {
  return apiClient.get<BusinessHoursDto>('/admin/sla/business-hours')
}

export function updateBusinessHours(data: UpdateBusinessHoursRequest): Promise<BusinessHoursDto> {
  return apiClient.put<BusinessHoursDto>('/admin/sla/business-hours', data)
}
