interface SlaProgressBarProps {
  percentageElapsed: number
  isOverdue: boolean
  isResolved?: boolean
}

export function SlaProgressBar({ percentageElapsed, isOverdue, isResolved }: SlaProgressBarProps) {
  const displayPercentage = Math.min(percentageElapsed, 100)

  let barColor: string
  let textColor: string
  let label: string

  if (isResolved) {
    barColor = 'bg-emerald-500'
    textColor = 'text-emerald-600'
    label = 'Resuelto'
  } else if (isOverdue) {
    barColor = 'bg-red-500'
    textColor = 'text-red-600'
    label = `Retrasado (${Math.round(percentageElapsed)}%)`
  } else if (percentageElapsed >= 70) {
    barColor = 'bg-amber-500'
    textColor = 'text-amber-600'
    label = `${Math.round(percentageElapsed)}%`
  } else {
    barColor = 'bg-emerald-500'
    textColor = 'text-emerald-600'
    label = `${Math.round(percentageElapsed)}%`
  }

  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-center justify-between">
        <span className={`text-sm font-semibold ${textColor}`}>{label}</span>
      </div>
      <div className="h-3 w-full overflow-hidden rounded-full bg-gray-200">
        <div
          className={`h-full rounded-full transition-all duration-500 ${barColor} ${isOverdue ? 'animate-pulse' : ''}`}
          style={{ width: `${displayPercentage}%` }}
        />
      </div>
    </div>
  )
}
