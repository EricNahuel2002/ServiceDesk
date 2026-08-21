import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { Button } from '../../components/common/Button'
import { Card } from '../../components/common/Card'
import { Input } from '../../components/common/Input'
import { Select } from '../../components/common/Select'
import { Textarea } from '../../components/common/Textarea'
import { AppShell } from '../../components/layout/AppShell'
import { useCategories } from '../../features/catalog/queries'
import {
  ALLOWED_FILE_TYPES,
  MAX_FILES,
  MAX_FILE_SIZE,
  formatFileSize,
} from '../../features/tickets/constants'
import { useCreateTicket } from '../../features/tickets/queries'
import { requireCliente } from '../../features/tickets/auth'
import { ApiError } from '../../lib/apiClient'
import type { ApiErrorPayload } from '../../types/api'

export const Route = createFileRoute('/tickets/')({
  beforeLoad: () => requireCliente(),
  component: CreateTicketPage,
})

interface FormErrors {
  title?: string
  description?: string
  categoryId?: string
  files?: string
}

function getFieldError(payload: ApiErrorPayload | undefined, field: string): string | undefined {
  const key = Object.keys(payload?.errors ?? {}).find(
    (errorKey) => errorKey.toLowerCase() === field.toLowerCase(),
  )
  return key ? payload?.errors?.[key]?.[0] : undefined
}

function CreateTicketPage() {
  const navigate = useNavigate()
  const categories = useCategories()
  const createTicket = useCreateTicket()

  const fileInputRef = useRef<HTMLInputElement>(null)

  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [files, setFiles] = useState<File[]>([])
  const [errors, setErrors] = useState<FormErrors>({})

  const categoriesList = categories.data ?? []

  function validateFiles(newFiles: File[]): string | undefined {
    const total = files.length + newFiles.length

    if (total > MAX_FILES) {
      return `No se pueden adjuntar más de ${MAX_FILES} archivos por ticket.`
    }

    const invalidType = newFiles.find((file) => !ALLOWED_FILE_TYPES.includes(file.type))
    if (invalidType) {
      return `El archivo "${invalidType.name}" tiene un tipo no permitido.`
    }

    const oversized = newFiles.find((file) => file.size > MAX_FILE_SIZE)
    if (oversized) {
      return `El archivo "${oversized.name}" supera el límite de 50 MB.`
    }

    return undefined
  }

  function handleFilesChange(event: ChangeEvent<HTMLInputElement>) {
    const incoming = Array.from(event.target.files ?? [])

    const fileError = validateFiles(incoming)
    if (fileError) {
      setErrors((current) => ({ ...current, files: fileError }))
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
      return
    }

    setErrors((current) => ({ ...current, files: undefined }))
    setFiles((current) => [...current, ...incoming])
    if (fileInputRef.current) {
      fileInputRef.current.value = ''
    }
  }

  function removeFile(index: number) {
    setFiles((current) => current.filter((_, fileIndex) => fileIndex !== index))
  }

  function validateForm(): FormErrors {
    const nextErrors: FormErrors = {}

    if (!title.trim()) {
      nextErrors.title = 'El título es obligatorio.'
    } else if (title.length > 200) {
      nextErrors.title = 'El título no puede superar los 200 caracteres.'
    }

    if (!description.trim()) {
      nextErrors.description = 'La descripción es obligatoria.'
    }

    if (!categoryId) {
      nextErrors.categoryId = 'Seleccioná una categoría.'
    }

    return nextErrors
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const nextErrors = validateForm()
    setErrors(nextErrors)

    if (Object.keys(nextErrors).length > 0) {
      return
    }

    createTicket.mutate(
      { title, description, categoryId, files },
      {
        onSuccess: () => {
          void navigate({ to: '/tickets/mis-tickets' })
        },
      },
    )
  }

  const apiErrorPayload = createTicket.error instanceof ApiError
    ? createTicket.error.payload
    : undefined

  return (
    <AppShell>
      <h1 className="mb-6 text-center text-2xl font-bold text-gray-900">Nuevo ticket</h1>
      <Card className="mx-auto max-w-2xl">
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <Input
            label="Título"
            name="title"
            required
            maxLength={200}
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            error={errors.title ?? getFieldError(apiErrorPayload, 'title')}
          />

          <Select
            label="Categoría"
            name="categoryId"
            required
            value={categoryId}
            onChange={(event) => setCategoryId(event.target.value)}
            error={errors.categoryId ?? getFieldError(apiErrorPayload, 'categoryId')}
          >
            <option value="" disabled>
              {categories.isPending ? 'Cargando categorías...' : 'Seleccioná una categoría'}
            </option>
            {categoriesList.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </Select>

          <Textarea
            label="Descripción"
            name="description"
            rows={6}
            required
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            error={errors.description ?? getFieldError(apiErrorPayload, 'description')}
          />

          <div className="flex flex-col gap-1">
            <label htmlFor="files" className="text-sm font-medium text-gray-700">
              Archivos adjuntos
            </label>
            <input
              ref={fileInputRef}
              id="files"
              name="files"
              type="file"
              multiple
              accept={ALLOWED_FILE_TYPES.join(',')}
              onChange={handleFilesChange}
              className="block w-full cursor-pointer text-sm text-gray-600 file:mr-3 file:cursor-pointer file:rounded-md file:border-0 file:bg-emerald-50 file:px-3 file:py-2 file:text-sm file:font-medium file:text-emerald-700 hover:file:bg-emerald-100"
            />
            <p className="text-xs text-gray-500">
              Máximo {MAX_FILES} archivos de hasta 50 MB cada uno (imágenes o videos).
            </p>
            {errors.files ? <p className="text-sm text-red-600">{errors.files}</p> : null}
          </div>

          {files.length > 0 ? (
            <ul className="flex flex-col gap-2">
              {files.map((file, index) => (
                <li
                  key={`${file.name}-${file.lastModified}-${index}`}
                  className="flex items-center justify-between gap-3 rounded-md border border-gray-200 bg-gray-50 px-3 py-2 text-sm"
                >
                  <span className="min-w-0 truncate text-gray-700">{file.name}</span>
                  <span className="shrink-0 text-gray-500">{formatFileSize(file.size)}</span>
                  <button
                    type="button"
                    onClick={() => removeFile(index)}
                    className="shrink-0 text-sm text-red-600 hover:text-red-700"
                  >
                    Quitar
                  </button>
                </li>
              ))}
            </ul>
          ) : null}

          {apiErrorPayload?.detail && !apiErrorPayload.errors ? (
            <p className="text-sm text-red-600">{apiErrorPayload.detail}</p>
          ) : null}

          {apiErrorPayload?.title && !apiErrorPayload.errors && !apiErrorPayload.detail ? (
            <p className="text-sm text-red-600">{apiErrorPayload.title}</p>
          ) : null}

          <Button type="submit" disabled={createTicket.isPending || categories.isPending}>
            {createTicket.isPending ? 'Creando ticket...' : 'Crear ticket'}
          </Button>
        </form>
      </Card>
    </AppShell>
  )
}
