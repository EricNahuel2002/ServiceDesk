import { useCallback, useMemo } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useStatuses } from '../catalog/queries'
import { getAllTickets, getTicketById, getTechnicians, updateTicket } from './api'
import type { TicketDto, UpdateTicketRequest } from '../tickets/types'

export function useAdminTickets() {
  return useQuery({ queryKey: ['admin', 'tickets'], queryFn: getAllTickets })
}

export function useAdminTicket(id: string) {
  return useQuery({
    queryKey: ['admin', 'tickets', id],
    queryFn: () => getTicketById(id),
    enabled: Boolean(id),
  })
}

export function useTechnicians() {
  return useQuery({ queryKey: ['admin', 'technicians'], queryFn: getTechnicians })
}

export function useUpdateTicket() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTicketRequest }) =>
      updateTicket(id, data),
    onSuccess: (_result, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'tickets'] })
      void queryClient.invalidateQueries({ queryKey: ['admin', 'tickets', variables.id] })
    },
  })
}

export function useIsTicketClosed() {
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
