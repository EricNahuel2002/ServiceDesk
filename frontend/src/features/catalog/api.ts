import { apiClient } from '../../lib/apiClient'
import type { CategoryDto, PriorityDto, StatusDto } from './types'

export function getCategories(): Promise<CategoryDto[]> {
  return apiClient.get<CategoryDto[]>('/catalog/categories')
}

export function getPriorities(): Promise<PriorityDto[]> {
  return apiClient.get<PriorityDto[]>('/catalog/priorities')
}

export function getStatuses(): Promise<StatusDto[]> {
  return apiClient.get<StatusDto[]>('/catalog/statuses')
}
