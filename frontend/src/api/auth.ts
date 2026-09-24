import type {
  LoginRequest,
  LoginResponse,
  RegisterUserRequest,
  UserResponse,
} from '../types/auth'
import { api } from './client'

export async function register(
  request: RegisterUserRequest,
): Promise<UserResponse> {
  const { data } = await api.post<UserResponse>('/auth/register', request)
  return data
}

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const { data } = await api.post<LoginResponse>('/auth/login', request)
  return data
}
