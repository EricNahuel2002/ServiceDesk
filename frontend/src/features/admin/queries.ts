import { useCallback, useMemo } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useStatuses } from '../catalog/queries'
import {
  getAllTickets,
  getTicketById,
  getTechnicians,
  updateTicket,
  getAllCategories,
  createCategory,
  updateCategory,
  getAllPriorities,
  createPriority,
  updatePriority,
  getAllStatuses,
  createStatus,
  updateStatus,
} from './api'
import type { TicketDto, UpdateTicketRequest } from '../tickets/types'
import type {
  CreateCategoryRequest,
  UpdateCategoryRequest,
  CreatePriorityRequest,
  UpdatePriorityRequest,
  CreateStatusRequest,
  UpdateStatusRequest,
} from './types'

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

export function useAdminCategories() {
  return useQuery({ queryKey: ['admin', 'categories'], queryFn: getAllCategories })
}

export function useAdminPriorities() {
  return useQuery({ queryKey: ['admin', 'priorities'], queryFn: getAllPriorities })
}

export function useAdminStatuses() {
  return useQuery({ queryKey: ['admin', 'statuses'], queryFn: getAllStatuses })
}

export function useCreateCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateCategoryRequest) => createCategory(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] })
    },
  })
}

export function useUpdateCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateCategoryRequest }) =>
      updateCategory(id, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] })
    },
  })
}

export function useCreatePriority() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreatePriorityRequest) => createPriority(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'priorities'] })
    },
  })
}

export function useUpdatePriority() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdatePriorityRequest }) =>
      updatePriority(id, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'priorities'] })
    },
  })
}

export function useCreateStatus() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateStatusRequest) => createStatus(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'statuses'] })
    },
  })
}

export function useUpdateStatus() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateStatusRequest }) =>
      updateStatus(id, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'statuses'] })
    },
  })
}
