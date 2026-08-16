import { createFileRoute, Link } from '@tanstack/react-router'
import { useMemo, useState } from 'react'
import { Card } from '../../../components/common/Card'
import { Badge } from '../../../components/common/Badge'
import { Select } from '../../../components/common/Select'
import { Button } from '../../../components/common/Button'
import { AdminAppShell } from '../../../components/layout/AdminAppShell'
import { useAdminTicket, useTechnicians, useUpdateTicket } from '../../../features/admin/queries'
import { usePriorities, useStatuses } from '../../../features/catalog/queries'
import { formatDate } from '../../../utils/format'

export const Route = createFileRoute('/admin/tickets/$ticketId')({
  component: AdminTicketDetailPage,
})

function getStatusBadgeColor(statusName: string): 'blue' | 'amber' | 'green' | 'red' | 'gray' {
  const lower = statusName.toLowerCase()
  if (lower.includes('nuevo') || lower.includes('abierto') || lower.includes('new') || lower.includes('open'))
    return 'blue'
  if (lower.includes('progreso') || lower.includes('asignad') || lower.includes('progress') || lower.includes('assigned'))
    return 'amber'
  if (lower.includes('resuelto') || lower.includes('finalizado') || lower.includes('closed') || lower.includes('resolved'))
    return 'green'
  if (lower.includes('cancelado') || lower.includes('cerrado') || lower.includes('cancelled') || lower.includes('canceled'))
    return 'red'
  return 'gray'
}

function getPriorityBadgeColor(priorityName: string): 'red' | 'amber' | 'green' | 'gray' {
  const lower = priorityName.toLowerCase()
  if (lower.includes('alta') || lower.includes('high') || lower.includes('urgente'))
    return 'red'
  if (lower.includes('media') || lower.includes('medium') || lower.includes('normal'))
    return 'amber'
  if (lower.includes('baja') || lower.includes('low'))
    return 'green'
  return 'gray'
}

function AdminTicketDetailPage() {
  const { ticketId } = Route.useParams()
  const ticket = useAdminTicket(ticketId)
  const technicians = useTechnicians()
  const priorities = usePriorities()
  const statuses = useStatuses()
  const updateTicket = useUpdateTicket()

  const initialValues = useMemo(
    () =>
      ticket.data
        ? {
            assignedToId: ticket.data.assignedToId ?? '',
            priorityId: ticket.data.priorityId,
            statusId: ticket.data.statusId,
          }
        : undefined,
    [ticket.data],
  )

  const [overrides, setOverrides] = useState<Record<string, string>>({})

  const assignedToId = overrides.assignedToId ?? initialValues?.assignedToId ?? ''
  const priorityId = overrides.priorityId ?? initialValues?.priorityId ?? ''
  const statusId = overrides.statusId ?? initialValues?.statusId ?? ''

  const hasChanges = Boolean(initialValues) && (
    assignedToId !== initialValues!.assignedToId ||
    priorityId !== initialValues!.priorityId ||
    statusId !== initialValues!.statusId
  )

  function handleFieldChange(field: string, value: string) {
    setOverrides((prev) => ({ ...prev, [field]: value }))
  }

  function handleSave() {
    if (!hasChanges) return
    updateTicket.mutate({
      id: ticketId,
      data: { assignedToId, priorityId, statusId },
    })
  }

  if (ticket.isPending) {
    return (
      <AdminAppShell>
        <p className="text-gray-500">Cargando...</p>
      </AdminAppShell>
    )
  }

  if (ticket.error || !ticket.data) {
    return (
      <AdminAppShell>
        <p className="text-red-600">Error al cargar el ticket.</p>
      </AdminAppShell>
    )
  }

  const t = ticket.data

  return (
    <AdminAppShell>
      <div className="mb-6 flex items-center gap-4">
        <Link
          to="/admin"
          className="text-sm font-medium text-[#0F52BA] hover:underline"
        >
          ← Volver
        </Link>
        <h1 className="text-2xl font-bold text-gray-900">{t.title}</h1>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2 flex flex-col gap-6">
          <Card>
            <h2 className="mb-4 text-lg font-semibold text-gray-900">Información del ticket</h2>
            <div className="flex flex-col gap-4">
              <div>
                <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                  Descripción
                </span>
                <p className="mt-1 text-sm text-gray-900 whitespace-pre-wrap">{t.description}</p>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-1">
                  <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                    Categoría
                  </span>
                  <span className="text-sm font-medium text-gray-900">{t.categoryName}</span>
                </div>
                <div className="flex flex-col gap-1">
                  <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                    Estado actual
                  </span>
                  <Badge color={getStatusBadgeColor(t.statusName)}>{t.statusName}</Badge>
                </div>
                <div className="flex flex-col gap-1">
                  <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                    Prioridad actual
                  </span>
                  <Badge color={getPriorityBadgeColor(t.priorityName)}>{t.priorityName}</Badge>
                </div>
                <div className="flex flex-col gap-1">
                  <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                    Técnico actual
                  </span>
                  {t.assignedToFirstName ? (
                    <span className="text-sm font-medium text-gray-900">
                      {t.assignedToFirstName} {t.assignedToLastName}
                    </span>
                  ) : (
                    <span className="text-sm text-gray-400">Sin asignar</span>
                  )}
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-1">
                  <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                    Creado
                  </span>
                  <span className="text-sm font-medium text-gray-900">
                    {formatDate(t.createdAtUtc)}
                  </span>
                </div>
                {t.updatedAtUtc && (
                  <div className="flex flex-col gap-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                      Actualizado
                    </span>
                    <span className="text-sm font-medium text-gray-900">
                      {formatDate(t.updatedAtUtc)}
                    </span>
                  </div>
                )}
              </div>
            </div>
          </Card>

          {t.attachments.length > 0 && (
            <Card>
              <h2 className="mb-4 text-lg font-semibold text-gray-900">Archivos adjuntos</h2>
              <ul className="flex flex-col gap-2">
                {t.attachments.map((att) => (
                  <li
                    key={att.id}
                    className="flex items-center justify-between rounded-md border border-gray-200 px-4 py-2"
                  >
                    <span className="text-sm text-gray-900">{att.fileName}</span>
                    <span className="text-xs text-gray-500">
                      {(att.sizeInBytes / 1024).toFixed(1)} KB
                    </span>
                  </li>
                ))}
              </ul>
            </Card>
          )}
        </div>

        <div className="flex flex-col gap-6">
          <Card>
            <h2 className="mb-4 text-lg font-semibold text-gray-900">Asignar valores</h2>
            <div className="flex flex-col gap-4">
              <Select
                label="Técnico"
                value={assignedToId}
                onChange={(e) => handleFieldChange('assignedToId', e.target.value)}
              >
                <option value="">Sin asignar</option>
                {technicians.data?.map((tech) => (
                  <option key={tech.id} value={tech.id}>
                    {tech.firstName} {tech.lastName}
                  </option>
                ))}
              </Select>

              <Select
                label="Prioridad"
                value={priorityId}
                onChange={(e) => handleFieldChange('priorityId', e.target.value)}
              >
                {priorities.data
                  ?.filter((p) => p.isActive)
                  .map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name}
                    </option>
                  ))}
              </Select>

              <Select
                label="Estado"
                value={statusId}
                onChange={(e) => handleFieldChange('statusId', e.target.value)}
              >
                {statuses.data
                  ?.filter((s) => s.isActive)
                  .map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name}
                    </option>
                  ))}
              </Select>

              <Button
                disabled={!hasChanges || updateTicket.isPending}
                onClick={handleSave}
              >
                {updateTicket.isPending ? 'Guardando...' : 'Guardar cambios'}
              </Button>

              {updateTicket.isSuccess && (
                <p className="text-sm text-green-600">Cambios guardados correctamente.</p>
              )}
              {updateTicket.isError && (
                <p className="text-sm text-red-600">Error al guardar los cambios.</p>
              )}
            </div>
          </Card>
        </div>
      </div>
    </AdminAppShell>
  )
}
