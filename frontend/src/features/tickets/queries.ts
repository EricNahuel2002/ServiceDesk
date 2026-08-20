import { useCallback } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { queryClient } from '../../lib/queryClient'
import { createTicket, getMyTickets } from './api'
import type { TicketDto } from './types'

export function useTickets() {
  return useQuery({ queryKey: ['tickets'], queryFn: getMyTickets })
}

export function useIsTicketClosed() {
  const isClosed = useCallback(
    (ticket: TicketDto) => ticket.resolvedAtUtc !== null,
    [],
  )

  return { isClosed }
}

export function useCreateTicket() {
  return useMutation({
    mutationFn: createTicket,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['tickets'] })
    },
  })
}
