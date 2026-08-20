import { apiClient } from '../../lib/apiClient'
import type { CategoryDto } from './types'

export function getCategories(): Promise<CategoryDto[]> {
  return apiClient.get<CategoryDto[]>('/catalog/categories')
}
