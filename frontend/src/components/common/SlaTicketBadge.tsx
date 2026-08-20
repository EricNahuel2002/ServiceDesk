import { useEffect, useState } from 'react'

interface SlaTicketBadgeProps {
  responseDeadlineAtUtc: string
  startedWorkAtUtc: string | null
  slaPercentageElapsed: number
}

function formatCompact(totalMs: number): string {
  const absMs = Math.abs(totalMs)
  const hours = Math.floor(absMs / 3600000)
  const minutes = Math.floor((absMs % 3600000) / 60000)
  if (hours > 0) return `${hours}h ${minutes}m`
  return `${minutes}m`
}

export function SlaTicketBadge({
  responseDeadlineAtUtc,
  startedWorkAtUtc,
  slaPercentageElapsed,
}: SlaTicketBadgeProps) {
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    const interval = setInterval(() => setNow(Date.now()), 30000)
    return () => clearInterval(interval)
  }, [])

  if (!startedWorkAtUtc) {
    return (
      <span className="inline-flex items-center rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600">
        Sin iniciar
      </span>
    )
  }

  const deadlineMs = new Date(responseDeadlineAtUtc).getTime()
  const diffMs = deadlineMs - now

  if (diffMs > 0) {
    let colorClass: string
    if (slaPercentageElapsed >= 70) {
      colorClass = 'bg-amber-100 text-amber-700'
    } else {
      colorClass = 'bg-emerald-100 text-emerald-700'
    }
    return (
      <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${colorClass}`}>
        {formatCompact(diffMs)}
      </span>
    )
  }

  return (
    <span className="inline-flex items-center rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-700">
      Retraso +{formatCompact(diffMs)}
    </span>
  )
}
