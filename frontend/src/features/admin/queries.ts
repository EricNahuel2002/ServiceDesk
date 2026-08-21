import { useCallback } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
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
  getSlaConfigurations,
  updateSlaConfiguration,
  getBusinessHours,
  updateBusinessHours,
  getAuditTechnicians,
  getAuditTechnicianTickets,
  getAuditTicketHistory,
  getAuditTicketChat,
} from './api'
import type { TicketDto, UpdateTicketRequest, AssignTicketRequest } from '../tickets/types'
import type {
  CreateCategoryRequest,
  UpdateCategoryRequest,
  CreateUserRequest,
  UpdateSlaConfigurationRequest,
  UpdateBusinessHoursRequest,
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
  const isClosed = useCallback(
    (ticket: TicketDto) => ticket.resolvedAtUtc !== null,
    [],
  )

  return { isClosed }
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

export function useSlaConfigurations() {
  return useQuery({ queryKey: ['admin', 'sla', 'configurations'], queryFn: getSlaConfigurations })
}

export function useUpdateSlaConfiguration() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateSlaConfigurationRequest) => updateSlaConfiguration(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'sla', 'configurations'] })
    },
  })
}

export function useBusinessHours() {
  return useQuery({ queryKey: ['admin', 'sla', 'business-hours'], queryFn: getBusinessHours })
}

export function useUpdateBusinessHours() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateBusinessHoursRequest) => updateBusinessHours(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'sla', 'business-hours'] })
    },
  })
}

export function useAuditTechnicians() {
  return useQuery({ queryKey: ['admin', 'audits', 'technicians'], queryFn: getAuditTechnicians })
}

export function useAuditTechnicianTickets(technicianId: string) {
  return useQuery({
    queryKey: ['admin', 'audits', 'technicians', technicianId, 'tickets'],
    queryFn: () => getAuditTechnicianTickets(technicianId),
    enabled: Boolean(technicianId),
  })
}

export function useAuditTicketHistory(ticketId: string) {
  return useQuery({
    queryKey: ['admin', 'audits', 'tickets', ticketId, 'history'],
    queryFn: () => getAuditTicketHistory(ticketId),
    enabled: Boolean(ticketId),
  })
}

export function useAuditTicketChat(ticketId: string) {
  return useQuery({
    queryKey: ['admin', 'audits', 'tickets', ticketId, 'chat'],
    queryFn: () => getAuditTicketChat(ticketId),
    enabled: Boolean(ticketId),
  })
}
