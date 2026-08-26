import { useState } from 'react'

interface StarRatingProps {
  value: number | null
  onChange: (value: number) => void
  disabled?: boolean
}

const RATING_LABELS: Record<number, string> = {
  1: 'Muy malo',
  2: 'Malo',
  3: 'Regular',
  4: 'Bueno',
  5: 'Excelente',
}

export function StarRating({ value, onChange, disabled = false }: StarRatingProps) {
  const [hovered, setHovered] = useState<number | null>(null)

  const displayed = hovered ?? value ?? 0

  return (
    <div className="flex flex-col gap-1">
      <div className="flex gap-1" role="radiogroup" aria-label="Calificación">
        {[1, 2, 3, 4, 5].map((star) => (
          <button
            key={star}
            type="button"
            role="radio"
            aria-checked={value === star}
            aria-label={`${star} ${star === 1 ? 'estrella' : 'estrellas'}`}
            disabled={disabled}
            onMouseEnter={() => setHovered(star)}
            onMouseLeave={() => setHovered(null)}
            onClick={() => onChange(star)}
            className={`text-2xl leading-none transition-colors ${
              disabled ? 'cursor-not-allowed' : 'cursor-pointer'
            } ${star <= displayed ? 'text-amber-400' : 'text-gray-300'}`}
          >
            ★
          </button>
        ))}
      </div>
      {displayed > 0 && <p className="text-xs text-gray-500">{RATING_LABELS[displayed]}</p>}
    </div>
  )
}
