import { apiClient } from '../../lib/apiClient'
import type { TicketDto, TechnicianDto, UpdateTicketRequest } from '../tickets/types'
import type {
  CreateCategoryRequest,
  UpdateCategoryRequest,
  CreateStatusRequest,
  UpdateStatusRequest,
  CreateUserRequest,
} from './types'
import type { CategoryDto, StatusDto } from '../catalog/types'
import type { UserListItemDto } from './types'
import type { AdminMetricsDto } from './types'

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

export function getAllCategories(): Promise<CategoryDto[]> {
  return apiClient.get<CategoryDto[]>('/admin/catalog/categories')
}

export function createCategory(data: CreateCategoryRequest): Promise<CategoryDto> {
  return apiClient.post<CategoryDto>('/admin/catalog/categories', data)
}

export function updateCategory(id: string, data: UpdateCategoryRequest): Promise<CategoryDto> {
  return apiClient.put<CategoryDto>(`/admin/catalog/categories/${id}`, data)
}

export function getAllStatuses(): Promise<StatusDto[]> {
  return apiClient.get<StatusDto[]>('/admin/catalog/statuses')
}

export function createStatus(data: CreateStatusRequest): Promise<StatusDto> {
  return apiClient.post<StatusDto>('/admin/catalog/statuses', data)
}

export function updateStatus(id: string, data: UpdateStatusRequest): Promise<StatusDto> {
  return apiClient.put<StatusDto>(`/admin/catalog/statuses/${id}`, data)
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
}): Promise<AdminMetricsDto> {
  const searchParams = new URLSearchParams()
  if (params.from) searchParams.set('from', params.from)
  if (params.to) searchParams.set('to', params.to)
  if (params.technicianId) searchParams.set('technicianId', params.technicianId)
  const query = searchParams.toString()
  return apiClient.get<AdminMetricsDto>(`/admin/metrics${query ? `?${query}` : ''}`)
}
