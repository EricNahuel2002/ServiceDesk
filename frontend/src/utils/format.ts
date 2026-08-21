export function formatDate(value: string): string {
  const date = new Date(value)
  const hasTimezone = value.endsWith('Z') || value.includes('+') || /\d{2}:\d{2}$/.test(value)
  const utcDate = hasTimezone ? date : new Date(date.getTime() + date.getTimezoneOffset() * 60000)

  return new Intl.DateTimeFormat('es', {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone || 'America/Argentina/Buenos_Aires',
  }).format(utcDate)
}

export function formatTime(value: string): string {
  const date = new Date(value)
  const hasTimezone = value.endsWith('Z') || value.includes('+') || /\d{2}:\d{2}$/.test(value)
  const utcDate = hasTimezone ? date : new Date(date.getTime() + date.getTimezoneOffset() * 60000)

  return new Intl.DateTimeFormat('es', {
    hour: '2-digit',
    minute: '2-digit',
    timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone || 'America/Argentina/Buenos_Aires',
  }).format(utcDate)
}
