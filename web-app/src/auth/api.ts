import { apiClient } from '@/lib/apiClient'
import type { AuthSession, LoginRequest, LoginResponseDto } from '@/auth/types'
import { userRoleFromValue } from '@/auth/types'

export async function login(request: LoginRequest): Promise<AuthSession> {
  const { data } = await apiClient.post<LoginResponseDto>('/api/v1/auth/login', request)

  return {
    token: data.accessToken,
    expiresAt: data.expiresAt,
    userId: data.user.id,
    firstName: data.user.firstName,
    lastName: data.user.lastName,
    email: data.user.email,
    role: userRoleFromValue(data.user.role),
  }
}
