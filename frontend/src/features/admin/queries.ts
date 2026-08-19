import { useCallback, useMemo } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useStatuses } from '../catalog/queries'
import {
  getAllTickets,
  getTicketById,
  getTechnicians,
  updateTicket,
  assignTicket,
  getAllCategories,
  createCategory,
  updateCategory,
  getAllUsers,
  createUser,
  getMetrics,
} from './api'
import type { TicketDto, UpdateTicketRequest, AssignTicketRequest } from '../tickets/types'
import type {
  CreateCategoryRequest,
  UpdateCategoryRequest,
  CreateUserRequest,
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

export function useAssignTicket() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: AssignTicketRequest }) =>
      assignTicket(id, data),
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

export function useAdminUsers() {
  return useQuery({ queryKey: ['admin', 'users'], queryFn: getAllUsers })
}

export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateUserRequest) => createUser(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
    },
  })
}

export function useMetrics(params: { from?: string; to?: string; technicianId?: string; period?: string }) {
  return useQuery({
    queryKey: ['admin', 'metrics', params.from, params.to, params.technicianId, params.period],
    queryFn: () => getMetrics(params),
  })
}
