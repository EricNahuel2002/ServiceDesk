import { useQuery } from '@tanstack/react-query'
import { getCategories, getPriorities, getStatuses } from './api'

export function useCategories() {
  return useQuery({ queryKey: ['categories'], queryFn: getCategories })
}

export function usePriorities() {
  return useQuery({ queryKey: ['priorities'], queryFn: getPriorities })
}

export function useStatuses() {
  return useQuery({ queryKey: ['statuses'], queryFn: getStatuses })
}
