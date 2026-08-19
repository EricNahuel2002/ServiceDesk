import { useEffect, useState } from 'react'
import { SlaDelayBar } from './SlaDelayBar'

interface SlaCountdownTimerProps {
  responseDeadlineAtUtc: string
  startedWorkAtUtc: string | null
  assignedAtUtc?: string | null
  delayMinutes?: number
}

function formatDuration(totalMs: number): string {
  const absMs = Math.abs(totalMs)
  const hours = Math.floor(absMs / 3600000)
  const minutes = Math.floor((absMs % 3600000) / 60000)
  const seconds = Math.floor((absMs % 60000) / 1000)
  return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
}

export function SlaCountdownTimer({
  responseDeadlineAtUtc,
  startedWorkAtUtc,
  assignedAtUtc,
  delayMinutes,
}: SlaCountdownTimerProps) {
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    const interval = setInterval(() => setNow(Date.now()), 1000)
    return () => clearInterval(interval)
  }, [])

  if (!startedWorkAtUtc) {
    return (
      <div className="flex flex-col gap-2">
        <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
          <div className="flex items-center gap-3">
            <div className="h-3 w-3 rounded-full bg-gray-400" />
            <span className="text-sm font-medium text-gray-500">
              Esperando inicio de trabajo...
            </span>
          </div>
        </div>
        {assignedAtUtc && delayMinutes !== undefined && (
          <SlaDelayBar
            assignedAtUtc={assignedAtUtc}
            startedWorkAtUtc={startedWorkAtUtc}
            delayMinutes={delayMinutes}
          />
        )}
      </div>
    )
  }

  const deadlineMs = new Date(responseDeadlineAtUtc).getTime()
  const diffMs = deadlineMs - now

  if (diffMs > 0) {
    return (
      <div className="flex flex-col gap-2">
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium uppercase tracking-wide text-emerald-600">
              Tiempo restante
            </span>
            <span className="font-mono text-2xl font-bold text-emerald-700">
              {formatDuration(diffMs)}
            </span>
          </div>
        </div>
        {assignedAtUtc && delayMinutes !== undefined && (
          <SlaDelayBar
            assignedAtUtc={assignedAtUtc}
            startedWorkAtUtc={startedWorkAtUtc}
            delayMinutes={delayMinutes}
          />
        )}
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-2">
      <div className="animate-pulse rounded-lg border border-red-200 bg-red-50 p-4">
        <div className="flex items-center justify-between">
          <span className="text-xs font-medium uppercase tracking-wide text-red-600">
            Tiempo de retraso
          </span>
          <span className="font-mono text-2xl font-bold text-red-700">
            +{formatDuration(diffMs)}
          </span>
        </div>
      </div>
      {assignedAtUtc && delayMinutes !== undefined && (
        <SlaDelayBar
          assignedAtUtc={assignedAtUtc}
          startedWorkAtUtc={startedWorkAtUtc}
          delayMinutes={delayMinutes}
        />
      )}
    </div>
  )
}
