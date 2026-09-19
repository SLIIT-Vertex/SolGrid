import { apiClient } from '@/lib/apiClient'
import { AccountStatusValue, UserRoleValue, accountStatusFromValue, userRoleFromValue } from '@/auth/types'
import type {
  CreateUserInput,
  PagedResultDto,
  UpdateUserInput,
  User,
  UserDto,
  UserFilters,
} from '@/features/users/types'

function toUser(dto: UserDto): User {
  return {
    id: dto.id,
    firstName: dto.firstName,
    lastName: dto.lastName,
    email: dto.email,
    role: userRoleFromValue(dto.role),
    status: accountStatusFromValue(dto.status),
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt,
  }
}

export interface UserPage {
  items: User[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

export async function getUsers(filters: UserFilters): Promise<UserPage> {
  const { data } = await apiClient.get<PagedResultDto<UserDto>>('/api/v1/users', {
    params: {
      searchText: filters.searchText.trim() || undefined,
      role: filters.role ? UserRoleValue[filters.role] : undefined,
      status: filters.status ? AccountStatusValue[filters.status] : undefined,
      pageNumber: filters.pageNumber,
      pageSize: filters.pageSize,
    },
  })

  return {
    items: data.items.map(toUser),
    totalCount: data.totalCount,
    pageNumber: data.pageNumber,
    pageSize: data.pageSize,
  }
}

export async function getUser(id: string): Promise<User> {
  const { data } = await apiClient.get<UserDto>(`/api/v1/users/${id}`)
  return toUser(data)
}

export async function createUser(input: CreateUserInput): Promise<User> {
  const { data } = await apiClient.post<UserDto>('/api/v1/users', {
    ...input,
    role: UserRoleValue[input.role],
  })
  return toUser(data)
}

export async function updateUser(id: string, input: UpdateUserInput): Promise<User> {
  const { data } = await apiClient.put<UserDto>(`/api/v1/users/${id}`, {
    ...input,
    role: UserRoleValue[input.role],
  })
  return toUser(data)
}

export async function resetUserPassword(id: string, newPassword: string): Promise<void> {
  await apiClient.patch(`/api/v1/users/${id}/password`, { newPassword })
}

export async function deactivateUser(id: string): Promise<void> {
  await apiClient.patch(`/api/v1/users/${id}/deactivate`)
}

export async function reactivateUser(id: string): Promise<void> {
  await apiClient.patch(`/api/v1/users/${id}/reactivate`)
}
