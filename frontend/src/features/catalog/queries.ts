import { useQuery } from '@tanstack/react-query'
import { getCategories, getStatuses } from './api'

export function useCategories() {
  return useQuery({ queryKey: ['categories'], queryFn: getCategories })
}

export function useStatuses() {
  return useQuery({ queryKey: ['statuses'], queryFn: getStatuses })
}
