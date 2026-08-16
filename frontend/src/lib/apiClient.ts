import type { ApiErrorPayload } from '../types/api'

const API_BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? '/api'

let accessToken: string | null = null

export function setAccessToken(token: string | null): void {
  accessToken = token
}

interface RequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown
}

export class ApiError extends Error {
  readonly status: number
  readonly payload?: ApiErrorPayload

  constructor(status: number, payload?: ApiErrorPayload) {
    super(payload?.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.payload = payload
  }
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { body, headers, ...init } = options

  const isFormData = body instanceof FormData

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      ...(!isFormData ? { 'Content-Type': 'application/json' } : {}),
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...headers,
    },
    body: body === undefined ? undefined : isFormData ? body : JSON.stringify(body),
  })

  if (!response.ok) {
    const payload: ApiErrorPayload | undefined = await response
      .json()
      .catch(() => undefined)
    throw new ApiError(response.status, payload)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export const apiClient = {
  get: <T>(path: string): Promise<T> => request<T>(path),
  post: <T>(path: string, body?: unknown): Promise<T> =>
    request<T>(path, { method: 'POST', body }),
  postForm: <T>(path: string, formData: FormData): Promise<T> =>
    request<T>(path, { method: 'POST', body: formData }),
  put: <T>(path: string, body?: unknown): Promise<T> =>
    request<T>(path, { method: 'PUT', body }),
  patch: <T>(path: string, body?: unknown): Promise<T> =>
    request<T>(path, { method: 'PATCH', body }),
  delete: <T>(path: string): Promise<T> => request<T>(path, { method: 'DELETE' }),
}
