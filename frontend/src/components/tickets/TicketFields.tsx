import type { ReactNode } from 'react'
import { formatDate } from '../../utils/format'

interface TicketFieldsProps {
  category: string
  status: string
  statusClosed?: boolean
  createdAt: string
  assignedToFirstName?: string | null
  assignedToLastName?: string | null
  assignedToEmail?: string | null
  action?: ReactNode
}

export function TicketFields({
  category,
  status,
  statusClosed = false,
  createdAt,
  assignedToFirstName,
  assignedToLastName,
  assignedToEmail,
  action,
}: TicketFieldsProps) {
  const hasAssignedTo = Boolean(assignedToFirstName || assignedToLastName || assignedToEmail)

  return (
    <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
      <div className="flex flex-col gap-1">
        <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
          Categoría
        </span>
        <span className="text-sm font-medium text-gray-900">{category}</span>
      </div>
      <div className="flex flex-col gap-1">
        <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
          Estado
        </span>
        <span className={`text-sm font-medium ${statusClosed ? 'text-gray-400' : 'text-gray-900'}`}>
          {status}
        </span>
      </div>
      <div className="flex flex-col gap-1">
        <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
          Técnico
        </span>
        {hasAssignedTo ? (
          <>
            <span className="text-sm font-medium text-gray-900">
              {assignedToFirstName} {assignedToLastName}
            </span>
            <span className="text-xs text-gray-500">{assignedToEmail}</span>
          </>
        ) : (
          <span className="text-sm text-gray-400">Pendiente de asignar</span>
        )}
      </div>
      <div className="flex flex-col gap-1">
        <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
          Creado
        </span>
        <span className="text-sm font-medium text-gray-900">{formatDate(createdAt)}</span>
      </div>
      {hasAssignedTo && action ? <div className="col-span-2 sm:col-span-4">{action}</div> : null}
    </div>
  )
}
