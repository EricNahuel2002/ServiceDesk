import { useCallback, useMemo } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useStatuses } from '../catalog/queries'
import { getAssignedTickets, resolveTicket } from './api'
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
  const statuses = useStatuses()

  const closedStatusIds = useMemo(
    () =>
      new Set(
        (statuses.data ?? [])
          .filter((status) => status.isClosed)
          .map((status) => status.id),
      ),
    [statuses.data],
  )

  const isClosed = useCallback(
    (ticket: TicketDto) => closedStatusIds.has(ticket.statusId),
    [closedStatusIds],
  )

  return { isClosed, statusesPending: statuses.isPending }
}
