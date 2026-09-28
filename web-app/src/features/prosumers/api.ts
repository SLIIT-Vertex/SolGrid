import { apiClient } from '@/lib/apiClient'
import { prosumerStatusFromValue, ProsumerAccountStatusValue } from '@/features/prosumers/types'
import type {
  PagedResultDto,
  Prosumer,
  ProsumerAccountStatus,
  ProsumerDto,
  ProsumerFilters,
  ProsumerStatusCounts,
  ProsumerActivity,
  ProsumerLifecycleChange,
} from '@/features/prosumers/types'

export function toProsumer(dto: ProsumerDto): Prosumer {
  return {
    nic: dto.nic,
    firstName: dto.firstName,
    lastName: dto.lastName,
    email: dto.email,
    phoneNumber: dto.phoneNumber,
    status: prosumerStatusFromValue(dto.status),
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt,
    version: dto.version,
  }
}

export interface ProsumerPage {
  items: Prosumer[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

export async function getProsumers(filters: ProsumerFilters): Promise<ProsumerPage> {
  const { data } = await apiClient.get<PagedResultDto<ProsumerDto>>('/api/v1/prosumers', {
    params: {
      searchText: filters.searchText || undefined,
      status: filters.status ? ProsumerAccountStatusValue[filters.status] : undefined,
      pageNumber: filters.pageNumber,
      pageSize: filters.pageSize,
    },
  })

  return {
    items: data.items.map(toProsumer),
    totalCount: data.totalCount,
    pageNumber: data.pageNumber,
    pageSize: data.pageSize,
  }
}

export async function getProsumer(nic: string): Promise<Prosumer> {
  const { data } = await apiClient.get<ProsumerDto>(`/api/v1/prosumers/${nic}`)
  return toProsumer(data)
}

const statusCountOrder: ProsumerAccountStatus[] = [
  'Pending',
  'Active',
  'DeactivationRequested',
  'Deactivated',
]

export async function getProsumerStatusCounts(): Promise<ProsumerStatusCounts> {
  const pages = await Promise.all(
    statusCountOrder.map((status) =>
      getProsumers({ searchText: '', status, pageNumber: 1, pageSize: 1 }),
    ),
  )

  return {
    Pending: pages[0].totalCount,
    Active: pages[1].totalCount,
    DeactivationRequested: pages[2].totalCount,
    Deactivated: pages[3].totalCount,
  }
}

export async function activateProsumer({ nic, ...request }: ProsumerLifecycleChange): Promise<void> {
  await apiClient.patch(`/api/v1/prosumers/${encodeURIComponent(nic)}/activate`, request)
}

export async function deactivateProsumer({ nic, ...request }: ProsumerLifecycleChange): Promise<void> {
  await apiClient.patch(`/api/v1/prosumers/${encodeURIComponent(nic)}/deactivate`, request)
}

export async function reactivateProsumer({ nic, ...request }: ProsumerLifecycleChange): Promise<void> {
  await apiClient.patch(`/api/v1/prosumers/${encodeURIComponent(nic)}/reactivate`, request)
}

export interface ProsumerProfileRequest {
  firstName: string
  lastName: string
  email: string
  phoneNumber: string | null
}

export async function createProsumer(request: ProsumerProfileRequest & { nic: string; password: string }): Promise<Prosumer> {
  const { data } = await apiClient.post<ProsumerDto>('/api/v1/prosumers', request)
  return toProsumer(data)
}

export async function updateProsumer(nic: string, request: ProsumerProfileRequest & { expectedVersion: number }): Promise<Prosumer> {
  const { firstName, lastName, email, phoneNumber, expectedVersion } = request
  const { data } = await apiClient.put<ProsumerDto>(`/api/v1/prosumers/${encodeURIComponent(nic)}`, { firstName, lastName, email, phoneNumber, expectedVersion })
  return toProsumer(data)
}

export async function getProsumerActivity(nic: string, pageNumber: number): Promise<PagedResultDto<ProsumerActivity>> {
  const { data } = await apiClient.get<PagedResultDto<ProsumerActivity>>(`/api/v1/prosumers/${encodeURIComponent(nic)}/activity`, {
    params: { pageNumber, pageSize: 10 },
  })
  return data
}
