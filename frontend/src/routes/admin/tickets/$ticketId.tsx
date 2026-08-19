import { createFileRoute, Link } from '@tanstack/react-router'
import { useMemo, useState } from 'react'
import { Card } from '../../../components/common/Card'
import { Badge } from '../../../components/common/Badge'
import { Select } from '../../../components/common/Select'
import { Button } from '../../../components/common/Button'
import { AdminAppShell } from '../../../components/layout/AdminAppShell'
import { useAdminTicket, useTechnicians, useUpdateTicket } from '../../../features/admin/queries'
import { requireAdmin } from '../../../features/admin/auth'
import { formatDate } from '../../../utils/format'
import { getPriorityLabel, getPriorityBadgeColor } from '../../../utils/priority'
import { getStatusBadgeColor } from '../../../utils/status'
import { SlaProgressBar } from '../../../components/common/SlaProgressBar'
import { SlaDelayBar } from '../../../components/common/SlaDelayBar'

export const Route = createFileRoute('/admin/tickets/$ticketId')({
  beforeLoad: () => requireAdmin(),
  component: AdminTicketDetailPage,
})

function AdminTicketDetailPage() {
  const { ticketId } = Route.useParams()
  const ticket = useAdminTicket(ticketId)
  const technicians = useTechnicians()
  const updateTicket = useUpdateTicket()

  const initialValues = useMemo(
    () =>
      ticket.data
        ? {
            assignedToId: ticket.data.assignedToId ?? '',
            priority: ticket.data.priority,
          }
        : undefined,
    [ticket.data],
  )

  const [overrides, setOverrides] = useState<Record<string, string | number | null>>({})

  const assignedToId = (overrides.assignedToId as string) ?? initialValues?.assignedToId ?? ''
  const priority = (overrides.priority as number | null) ?? initialValues?.priority ?? null

  const hasChanges = Boolean(initialValues) && (
    assignedToId !== initialValues!.assignedToId ||
    priority !== initialValues!.priority
  )

  function handleFieldChange(field: string, value: string | number | null) {
    setOverrides((prev) => ({ ...prev, [field]: value }))
  }

  function handleSave() {
    if (!hasChanges) return

    const data: Record<string, string | number | null> = {}

    if (assignedToId !== initialValues!.assignedToId) {
      data.assignedToId = assignedToId || null
    }
    if (priority !== initialValues!.priority) {
      data.priority = priority
    }

    updateTicket.mutate({ id: ticketId, data })
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
  const isResolved = t.statusName === 'Resuelto'

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
                  <Badge color={getPriorityBadgeColor(t.priority)}>{getPriorityLabel(t.priority)}</Badge>
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
                {t.startedWorkAtUtc && (
                  <div className="flex flex-col gap-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                      Inicio de trabajo
                    </span>
                    <span className="text-sm font-medium text-gray-900">
                      {formatDate(t.startedWorkAtUtc)}
                    </span>
                  </div>
                )}
                {t.resolvedAtUtc && (
                  <div className="flex flex-col gap-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                      Resuelto
                    </span>
                    <span className="text-sm font-medium text-gray-900">
                      {formatDate(t.resolvedAtUtc)}
                    </span>
                  </div>
                )}
              </div>
            </div>
          </Card>

          {t.assignedAtUtc && (
            <Card>
              <h2 className="mb-4 text-lg font-semibold text-gray-900">Estado SLA</h2>
              <div className="flex flex-col gap-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="flex flex-col gap-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                      Límite de respuesta
                    </span>
                    <span className="text-sm font-medium text-gray-900">
                      {t.slaLimitHours}h
                    </span>
                  </div>
                  <div className="flex flex-col gap-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                      Fecha límite
                    </span>
                    <span className="text-sm font-medium text-gray-900">
                      {formatDate(t.responseDeadlineAtUtc)}
                    </span>
                  </div>
                </div>
                <div className="flex flex-col gap-1.5">
                  <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
                    Progreso
                  </span>
                  <SlaProgressBar
                    percentageElapsed={t.resolvedAtUtc ? 100 : t.slaPercentageElapsed}
                    isOverdue={t.isOverdue}
                  />
                </div>
                <SlaDelayBar
                  assignedAtUtc={t.assignedAtUtc}
                  startedWorkAtUtc={t.startedWorkAtUtc}
                  delayMinutes={t.delayMinutes}
                />
              </div>
            </Card>
          )}

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

        {!isResolved && (
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
                  value={priority ?? ''}
                  onChange={(e) => handleFieldChange('priority', e.target.value ? Number(e.target.value) : null)}
                >
                  <option value="">Sin asignar</option>
                  <option value={1}>Baja</option>
                  <option value={2}>Media</option>
                  <option value={3}>Alta</option>
                  <option value={4}>Crítica</option>
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
        )}
      </div>
    </AdminAppShell>
  )
}
