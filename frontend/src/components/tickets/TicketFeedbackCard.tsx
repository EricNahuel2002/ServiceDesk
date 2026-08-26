import { useState } from 'react'
import { Button } from '../common/Button'
import { Card } from '../common/Card'
import { Textarea } from '../common/Textarea'
import { StarRating } from '../common/StarRating'
import { useSubmitFeedback } from '../../features/tickets/queries'
import { ApiError } from '../../lib/apiClient'

interface TicketFeedbackCardProps {
  ticketId: string
}

type Step = 'question' | 'review' | 'confirmReopen'

export function TicketFeedbackCard({ ticketId }: TicketFeedbackCardProps) {
  const submitFeedback = useSubmitFeedback()

  const [step, setStep] = useState<Step>('question')
  const [rating, setRating] = useState<number | null>(null)
  const [comment, setComment] = useState('')
  const [error, setError] = useState('')
  const [submitted, setSubmitted] = useState(false)

  function handleSubmit(wasSolved: boolean) {
    setError('')

    submitFeedback.mutate(
      {
        ticketId,
        input: {
          wasSolved,
          rating: wasSolved ? rating : null,
          comment: comment.trim() ? comment.trim() : null,
        },
      },
      {
        onSuccess: () => {
          setSubmitted(true)
        },
        onError: (mutationError) => {
          if (mutationError instanceof ApiError) {
            setError(mutationError.message)
          } else {
            setError('No se pudo enviar tu respuesta. Intentá nuevamente.')
          }
        },
      },
    )
  }

  function reset() {
    setStep('question')
    setRating(null)
    setComment('')
    setError('')
  }

  return (
    <Card>
      <div className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold text-gray-900">¿Se solucionó tu problema?</h3>

        {submitted ? (
          <p className="text-sm text-emerald-600">¡Gracias por tu respuesta!</p>
        ) : step === 'question' ? (
          <>
            <p className="text-sm text-gray-500">
              El técnico marcó este ticket como resuelto. Contanos si funcionó.
            </p>
            <div className="flex gap-2">
              <Button onClick={() => setStep('review')}>Sí</Button>
              <Button variant="secondary" onClick={() => setStep('confirmReopen')}>
                No
              </Button>
            </div>
          </>
        ) : step === 'review' ? (
          <>
            <StarRating value={rating} onChange={setRating} disabled={submitFeedback.isPending} />
            <Textarea
              label="Comentario (opcional)"
              placeholder="Contanos tu experiencia con la atención recibida..."
              value={comment}
              maxLength={2000}
              onChange={(event) => setComment(event.target.value)}
              disabled={submitFeedback.isPending}
            />
            {error && <p className="text-sm text-red-600">{error}</p>}
            <div className="flex gap-2">
              <Button onClick={() => handleSubmit(true)} disabled={submitFeedback.isPending}>
                {submitFeedback.isPending ? 'Enviando...' : 'Enviar reseña'}
              </Button>
              <Button variant="secondary" onClick={reset} disabled={submitFeedback.isPending}>
                Cancelar
              </Button>
            </div>
          </>
        ) : (
          <>
            <p className="text-sm text-gray-500">
              Si respondés que no, el ticket se reabrirá y quedará pendiente de que un
              administrador asigne un nuevo técnico.
            </p>
            {error && <p className="text-sm text-red-600">{error}</p>}
            <div className="flex gap-2">
              <Button
                onClick={() => handleSubmit(false)}
                disabled={submitFeedback.isPending}
                className="bg-red-500! hover:bg-red-600!"
              >
                {submitFeedback.isPending ? 'Reabriendo...' : 'Confirmar'}
              </Button>
              <Button variant="secondary" onClick={reset} disabled={submitFeedback.isPending}>
                Cancelar
              </Button>
            </div>
          </>
        )}
      </div>
    </Card>
  )
}
