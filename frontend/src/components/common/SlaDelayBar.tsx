import { useEffect, useState } from 'react'

interface SlaDelayBarProps {
  assignedAtUtc: string | null
  startedWorkAtUtc: string | null
  delayMinutes: number
}

function formatDelay(totalMinutes: number): string {
  const hours = Math.floor(totalMinutes / 60)
  const minutes = Math.floor(totalMinutes % 60)
  if (hours > 0) return `${hours}h ${minutes}m`
  return `${minutes}m`
}

export function SlaDelayBar({
  assignedAtUtc,
  startedWorkAtUtc,
  delayMinutes,
}: SlaDelayBarProps) {
  const [liveMinutes, setLiveMinutes] = useState(() => {
    if (!assignedAtUtc || startedWorkAtUtc) return 0
    return Math.floor((Date.now() - new Date(assignedAtUtc).getTime()) / 60000)
  })

  useEffect(() => {
    if (!assignedAtUtc || startedWorkAtUtc) return

    const interval = setInterval(() => {
      setLiveMinutes(Math.floor((Date.now() - new Date(assignedAtUtc).getTime()) / 60000))
    }, 1000)
    return () => clearInterval(interval)
  }, [assignedAtUtc, startedWorkAtUtc])

  if (!assignedAtUtc) return null

  const displayMinutes = startedWorkAtUtc ? delayMinutes : liveMinutes

  if (displayMinutes <= 0) return null

  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-center justify-between">
        <span className="text-sm font-semibold text-amber-600">
          Retraso: {formatDelay(displayMinutes)}
        </span>
      </div>
      <div className="h-2 w-full overflow-hidden rounded-full bg-gray-200">
        <div
          className="h-full rounded-full bg-amber-400 transition-all duration-1000"
          style={{ width: `${Math.min(displayMinutes, 60) / 60 * 100}%` }}
        />
      </div>
    </div>
  )
}
