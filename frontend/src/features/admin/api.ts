import { apiClient } from '../../lib/apiClient'
import type { TicketDto, TechnicianDto, UpdateTicketRequest } from '../tickets/types'
import type {
  CreateCategoryRequest,
  UpdateCategoryRequest,
  CreatePriorityRequest,
  UpdatePriorityRequest,
  CreateStatusRequest,
  UpdateStatusRequest,
} from './types'
import type { CategoryDto, PriorityDto, StatusDto } from '../catalog/types'

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

export function getAllPriorities(): Promise<PriorityDto[]> {
  return apiClient.get<PriorityDto[]>('/admin/catalog/priorities')
}

export function createPriority(data: CreatePriorityRequest): Promise<PriorityDto> {
  return apiClient.post<PriorityDto>('/admin/catalog/priorities', data)
}

export function updatePriority(id: string, data: UpdatePriorityRequest): Promise<PriorityDto> {
  return apiClient.put<PriorityDto>(`/admin/catalog/priorities/${id}`, data)
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
