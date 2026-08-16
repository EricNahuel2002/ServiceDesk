import { useCallback, useMemo } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { queryClient } from '../../lib/queryClient'
import { useStatuses } from '../catalog/queries'
import { createTicket, getMyTickets } from './api'
import type { TicketDto } from './types'

export function useTickets() {
  return useQuery({ queryKey: ['tickets'], queryFn: getMyTickets })
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

export function useCreateTicket() {
  return useMutation({
    mutationFn: createTicket,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['tickets'] })
    },
  })
}
