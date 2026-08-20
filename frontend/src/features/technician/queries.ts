import { useCallback } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getAssignedTickets, startWork, resolveTicket } from './api'
import type { TicketDto } from '../tickets/types'

export function useTechnicianTickets() {
  return useQuery({ queryKey: ['technician', 'tickets'], queryFn: getAssignedTickets })
}

export function useTechnicianTicket(id: string) {
  return useQuery({
    queryKey: ['technician', 'tickets', id],
    queryFn: async () => {
      const tickets = await getAssignedTickets()
      return tickets.find((t) => t.id === id) ?? null
    },
    enabled: Boolean(id),
  })
}

export function useStartWork() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => startWork(id),
    onSuccess: (_result, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['technician', 'tickets'] })
      void queryClient.invalidateQueries({ queryKey: ['technician', 'tickets', variables] })
    },
  })
}

export function useResolveTicket() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, resolutionNote }: { id: string; resolutionNote: string }) =>
      resolveTicket(id, resolutionNote),
    onSuccess: (_result, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['technician', 'tickets'] })
      void queryClient.invalidateQueries({ queryKey: ['technician', 'tickets', variables.id] })
    },
  })
}

export function useIsTechnicianTicketClosed() {
  const isClosed = useCallback(
    (ticket: TicketDto) => ticket.resolvedAtUtc !== null,
    [],
  )

  return { isClosed }
}
