import { useCallback } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { queryClient } from '../../lib/queryClient'
import { createTechnicianReport, createTicket, getMyTickets, submitFeedback } from './api'
import type {
  CreateTechnicianReportInput,
  SubmitTicketFeedbackRequest,
  TicketDto,
} from './types'

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

export function useSubmitFeedback() {
  return useMutation({
    mutationFn: ({ ticketId, input }: { ticketId: string; input: SubmitTicketFeedbackRequest }) =>
      submitFeedback(ticketId, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['tickets'] })
    },
  })
}

export function useCreateTechnicianReport() {
  return useMutation({
    mutationFn: ({
      ticketId,
      input,
    }: {
      ticketId: string
      input: CreateTechnicianReportInput
    }) => createTechnicianReport(ticketId, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['tickets'] })
    },
  })
}
