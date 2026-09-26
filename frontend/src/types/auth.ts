// Mirrors GameHub.Application.DTOs.Users

export interface UserResponse {
  id: string
  username: string
  email: string
  createdAt: string
}

// GET /api/users/search: only what is needed to pick someone.
export interface UserSearchResponse {
  id: string
  username: string
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

// Not a backend DTO: the logged-in user as rebuilt from the JWT claims
// (sub, unique_name, email). There is no /me endpoint.
export interface AuthUser {
  id: string
  username: string
  email: string
}
