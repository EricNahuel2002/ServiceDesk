import { useQuery } from '@tanstack/react-query'
import { getMyTickets } from './api'

export function useTickets() {
  return useQuery({ queryKey: ['tickets'], queryFn: getMyTickets })
}
