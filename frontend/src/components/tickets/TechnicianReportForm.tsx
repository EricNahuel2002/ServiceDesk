import { useRef, useState, type ChangeEvent } from 'react'
import { Button } from '../common/Button'
import { Card } from '../common/Card'
import { Textarea } from '../common/Textarea'
import { ChatMessageBubble } from '../chat/ChatMessageBubble'
import {
  ALLOWED_FILE_TYPES,
  MAX_FILES,
  MAX_FILE_SIZE,
  formatFileSize,
} from '../../features/tickets/constants'
import { useCreateTechnicianReport } from '../../features/tickets/queries'
import { useChatHistory } from '../../features/chat/queries'
import { useAuth } from '../../hooks/useAuth'
import { ApiError } from '../../lib/apiClient'

interface TechnicianReportFormProps {
  ticketId: string
}

export function TechnicianReportForm({ ticketId }: TechnicianReportFormProps) {
  const createReport = useCreateTechnicianReport()
  const { user } = useAuth()
  const [showChatPreview, setShowChatPreview] = useState(false)
  const chatHistory = useChatHistory(showChatPreview ? ticketId : '')

  const fileInputRef = useRef<HTMLInputElement>(null)

  const [reason, setReason] = useState('')
  const [files, setFiles] = useState<File[]>([])
  const [error, setError] = useState('')
  const [submitted, setSubmitted] = useState(false)

  function handleFilesChange(event: ChangeEvent<HTMLInputElement>) {
    const incoming = Array.from(event.target.files ?? [])

    if (files.length + incoming.length > MAX_FILES) {
      setError(`No se pueden adjuntar más de ${MAX_FILES} archivos.`)
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
      return
    }

    const invalidType = incoming.find((file) => !ALLOWED_FILE_TYPES.includes(file.type))
    if (invalidType) {
      setError(`El archivo "${invalidType.name}" tiene un tipo no permitido.`)
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
      return
    }

    const oversized = incoming.find((file) => file.size > MAX_FILE_SIZE)
    if (oversized) {
      setError(`El archivo "${oversized.name}" supera el límite de 50 MB.`)
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
      return
    }

    setError('')
    setFiles((current) => [...current, ...incoming])
    if (fileInputRef.current) {
      fileInputRef.current.value = ''
    }
  }

  function removeFile(index: number) {
    setFiles((current) => current.filter((_, fileIndex) => fileIndex !== index))
  }

  function handleSubmit() {
    setError('')

    createReport.mutate(
      {
        ticketId,
        input: {
          reason: reason.trim() ? reason.trim() : null,
          files,
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
            setError('No se pudo enviar el reporte. Intentá nuevamente.')
          }
        },
      },
    )
  }

  if (submitted) {
    return (
      <Card>
        <div className="flex flex-col gap-3">
          <h3 className="text-sm font-semibold text-gray-900">Reporte enviado</h3>
          <p className="text-sm text-emerald-600">
            Gracias. Los administradores revisarán tu reporte junto con la evidencia adjunta.
          </p>
        </div>
      </Card>
    )
  }

  return (
    <Card>
      <div className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold text-gray-900">¿Reportar técnico?</h3>
        <p className="text-sm text-gray-500">
          Opcional: podés reportar al técnico que atendió el ticket para que los administradores
          lo revisen.
        </p>

        <Textarea
          label="Motivo (opcional)"
          placeholder="Contanos qué ocurrió con la atención recibida..."
          value={reason}
          maxLength={2000}
          onChange={(event) => setReason(event.target.value)}
          disabled={createReport.isPending}
        />

        <div className="flex flex-col gap-2">
          <span className="text-sm font-medium text-gray-700">Evidencia (fotos o videos)</span>
          <input
            ref={fileInputRef}
            type="file"
            multiple
            accept={ALLOWED_FILE_TYPES.join(',')}
            onChange={handleFilesChange}
            disabled={createReport.isPending}
            className="block w-full cursor-pointer text-sm text-gray-600 file:mr-3 file:cursor-pointer file:rounded-md file:border-0 file:bg-emerald-50 file:px-3 file:py-2 file:text-sm file:font-medium file:text-emerald-700 hover:file:bg-emerald-100"
          />
          {files.length > 0 && (
            <ul className="flex flex-col gap-1">
              {files.map((file, index) => (
                <li key={`${file.name}-${index}`} className="flex items-center justify-between gap-2">
                  <span className="truncate text-sm text-gray-900">{file.name}</span>
                  <span className="flex shrink-0 items-center gap-2">
                    <span className="text-xs text-gray-500">{formatFileSize(file.size)}</span>
                    <button
                      type="button"
                      onClick={() => removeFile(index)}
                      className="text-xs font-medium text-red-600 hover:underline"
                      disabled={createReport.isPending}
                    >
                      Quitar
                    </button>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="flex flex-col gap-2 rounded-md border border-gray-200 p-3">
          <button
            type="button"
            onClick={() => setShowChatPreview((current) => !current)}
            className="flex w-fit items-center gap-1 text-sm font-medium text-[#0F52BA] hover:underline"
          >
            {showChatPreview ? 'Ocultar' : 'Ver'} conversación del chat
          </button>
          {showChatPreview && (
            <div className="flex max-h-64 flex-col gap-3 overflow-y-auto p-1">
              {chatHistory.isPending ? (
                <p className="text-sm text-gray-500">Cargando conversación...</p>
              ) : (chatHistory.data ?? []).length === 0 ? (
                <p className="text-sm text-gray-500">No hubo conversación en el chat.</p>
              ) : (
                [...(chatHistory.data ?? [])]
                  .sort(
                    (a, b) =>
                      new Date(a.sentAtUtc).getTime() - new Date(b.sentAtUtc).getTime(),
                  )
                  .map((message) => (
                    <ChatMessageBubble
                      key={message.id}
                      senderName={`${message.senderFirstName} ${message.senderLastName}`}
                      content={message.content}
                      sentAtUtc={message.sentAtUtc}
                      isOwn={message.senderId === user?.id}
                    />
                  ))
              )}
            </div>
          )}
        </div>

        {error && <p className="text-sm text-red-600">{error}</p>}

        <Button onClick={handleSubmit} disabled={createReport.isPending}>
          {createReport.isPending ? 'Enviando...' : 'Enviar reporte'}
        </Button>
      </div>
    </Card>
  )
}
