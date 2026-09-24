// Mirrors GameHub.Application.DTOs.Users

export interface UserResponse {
  id: string
  username: string
  email: string
  createdAt: string
}

export interface RegisterUserRequest {
  username: string
  email: string
  password: string
}

export interface LoginRequest {
  email: string
  password: string
}

// AuthController.Login returns an anonymous object: { token }
export interface LoginResponse {
  token: string
}
