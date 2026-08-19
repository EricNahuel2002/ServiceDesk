import { apiClient } from '../../lib/apiClient'
import type { CategoryDto, StatusDto } from './types'

export function getCategories(): Promise<CategoryDto[]> {
  return apiClient.get<CategoryDto[]>('/catalog/categories')
}

export function getStatuses(): Promise<StatusDto[]> {
  return apiClient.get<StatusDto[]>('/catalog/statuses')
}
