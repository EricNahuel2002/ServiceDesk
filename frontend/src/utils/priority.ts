export const TicketPriority = {
  Baja: 1,
  Media: 2,
  Alta: 3,
  Critica: 4,
} as const

export type TicketPriorityValue = (typeof TicketPriority)[keyof typeof TicketPriority]

export const PRIORITY_LABELS: Record<TicketPriorityValue, string> = {
  [TicketPriority.Baja]: 'Baja',
  [TicketPriority.Media]: 'Media',
  [TicketPriority.Alta]: 'Alta',
  [TicketPriority.Critica]: 'Crítica',
}

export function getPriorityLabel(priority: number | null): string {
  if (priority === null) return 'Sin asignar'
  return PRIORITY_LABELS[priority as TicketPriorityValue] ?? `Prioridad ${priority}`
}

export function getPriorityBadgeColor(priority: number | null): 'red' | 'amber' | 'green' | 'gray' {
  if (priority === null) return 'gray'
  switch (priority) {
    case TicketPriority.Critica:
    case TicketPriority.Alta:
      return 'red'
    case TicketPriority.Media:
      return 'amber'
    case TicketPriority.Baja:
      return 'green'
    default:
      return 'gray'
  }
}
