export function getStatusBadgeColor(statusName: string): 'blue' | 'amber' | 'green' | 'red' | 'gray' {
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
