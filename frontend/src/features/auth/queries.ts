import { useMutation } from '@tanstack/react-query'
import {
  login as loginApi,
  logout as logoutApi,
  register as registerApi,
} from './api'

export function useLogin() {
  return useMutation({ mutationFn: loginApi })
}

export function useRegister() {
  return useMutation({ mutationFn: registerApi })
}

export function useLogout() {
  return useMutation({ mutationFn: logoutApi })
}
