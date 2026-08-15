import { apiClient } from '../../lib/apiClient'
import type {
  AuthResponse,
  LoginRequest,
  LogoutRequest,
  RefreshTokenRequest,
  RegisterRequest,
} from './types'

export function login(request: LoginRequest): Promise<AuthResponse> {
  return apiClient.post<AuthResponse>('/auth/login', request)
}

export function register(request: RegisterRequest): Promise<AuthResponse> {
  return apiClient.post<AuthResponse>('/auth/register', request)
}

export function refreshToken(request: RefreshTokenRequest): Promise<AuthResponse> {
  return apiClient.post<AuthResponse>('/auth/refresh', request)
}

export function logout(request: LogoutRequest): Promise<void> {
  return apiClient.post<void>('/auth/logout', request)
}
