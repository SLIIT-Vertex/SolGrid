import { apiClient } from '@/lib/apiClient'
import { SlotStatusValue, StationStatusValue, slotStatusFromValue, stationStatusFromValue } from '@/features/microgrid/types'
import type {
  CreateBookingSlotRequest,
  CreateSolarStationRequest,
  EnergyBookingSlotDto,
  MicrogridBatterySlot,
  MicrogridNode,
  MicrogridNodeFilters,
  MicrogridSlotFilters,
  OperatingWindow,
  PagedResultDto,
  SolarStationDto,
  UpdateBookingSlotRequest,
  UpdateSolarStationRequest,
  UpdateStationScheduleRequest,
} from '@/features/microgrid/types'

function toOperatingWindow(dto: { day: number; opensAt: string; closesAt: string }): OperatingWindow {
  return { day: dto.day, opensAt: dto.opensAt, closesAt: dto.closesAt }
}

function toBatterySlot(dto: EnergyBookingSlotDto): MicrogridBatterySlot {
  return {
    id: dto.id,
    stationId: dto.stationId,
    slotNumber: dto.slotNumber,
    batteryCapacityKwh: dto.batteryCapacityKwh,
    startTime: dto.startTime,
    endTime: dto.endTime,
    status: slotStatusFromValue(dto.status),
    isActive: dto.isActive,
    isAvailable: dto.isAvailable,
  }
}

export async function getSlot(id: string): Promise<MicrogridBatterySlot> {
  const { data } = await apiClient.get<EnergyBookingSlotDto>(`/api/v1/slots/${encodeURIComponent(id)}`)
  return toBatterySlot(data)
}

function toMicrogridNode(dto: SolarStationDto): MicrogridNode {
  return {
    id: dto.id,
    code: dto.code,
    name: dto.name,
    addressLine: dto.addressLine,
    latitude: dto.location?.latitude ?? 0,
    longitude: dto.location?.longitude ?? 0,
    capacityKw: dto.capacityKw,
    status: stationStatusFromValue(dto.status),
    totalSlotCount: dto.totalSlotCount,
    availableSlotCount: dto.availableSlotCount,
    schedule: (dto.schedule ?? []).map(toOperatingWindow),
    slots: (dto.slots ?? []).map(toBatterySlot),
  }
}

export interface MicrogridNodePage {
  items: MicrogridNode[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

export async function listStations(filters: MicrogridNodeFilters): Promise<MicrogridNodePage> {
  const { data } = await apiClient.get<PagedResultDto<SolarStationDto>>('/api/v1/stations', {
    params: {
      searchText: filters.searchText.trim() || undefined,
      status: filters.status ? StationStatusValue[filters.status] : undefined,
      hasAvailableSlots:
        filters.hasAvailableSlots === 'available'
          ? true
          : filters.hasAvailableSlots === 'none'
            ? false
            : undefined,
      pageNumber: filters.pageNumber,
      pageSize: filters.pageSize,
    },
  })

  return {
    items: data.items.map(toMicrogridNode),
    totalCount: data.totalCount,
    pageNumber: data.pageNumber,
    pageSize: data.pageSize,
  }
}

export async function createStation(request: CreateSolarStationRequest): Promise<MicrogridNode> {
  const { data } = await apiClient.post<SolarStationDto>('/api/v1/stations', request)
  return toMicrogridNode(data)
}

export async function getStation(id: string): Promise<MicrogridNode> {
  const { data } = await apiClient.get<SolarStationDto>(`/api/v1/stations/${id}`)
  return toMicrogridNode(data)
}

export async function updateStation(id: string, request: UpdateSolarStationRequest): Promise<MicrogridNode> {
  const { data } = await apiClient.put<SolarStationDto>(`/api/v1/stations/${id}`, request)
  return toMicrogridNode(data)
}

export async function updateSchedule(id: string, request: UpdateStationScheduleRequest): Promise<MicrogridNode> {
  const { data } = await apiClient.put<SolarStationDto>(`/api/v1/stations/${id}/schedule`, request)
  return toMicrogridNode(data)
}

export async function activateStation(id: string): Promise<void> {
  await apiClient.patch(`/api/v1/stations/${id}/activate`)
}

export async function deactivateStation(id: string): Promise<void> {
  await apiClient.patch(`/api/v1/stations/${id}/deactivate`)
}

export interface MicrogridSlotPage {
  items: MicrogridBatterySlot[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

function nextUtcDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number)
  const next = new Date(Date.UTC(year, month - 1, day + 1))
  return next.toISOString().slice(0, 10)
}

export async function listSlots(stationId: string, filters: MicrogridSlotFilters): Promise<MicrogridSlotPage> {
  const { data } = await apiClient.get<PagedResultDto<EnergyBookingSlotDto>>(
    `/api/v1/stations/${stationId}/slots`,
    {
      params: {
        status: filters.status ? SlotStatusValue[filters.status] : undefined,
        from: filters.fromDate ? `${filters.fromDate}T00:00:00+00:00` : undefined,
        to: filters.toDate ? `${nextUtcDate(filters.toDate)}T00:00:00+00:00` : undefined,
        pageNumber: filters.pageNumber,
        pageSize: filters.pageSize,
      },
    },
  )

  return {
    items: data.items.map(toBatterySlot),
    totalCount: data.totalCount,
    pageNumber: data.pageNumber,
    pageSize: data.pageSize,
  }
}

export async function createSlot(
  stationId: string,
  request: CreateBookingSlotRequest,
): Promise<MicrogridBatterySlot> {
  const { data } = await apiClient.post<EnergyBookingSlotDto>(`/api/v1/stations/${stationId}/slots`, request)
  return toBatterySlot(data)
}

export async function updateSlot(id: string, request: UpdateBookingSlotRequest): Promise<MicrogridBatterySlot> {
  const { data } = await apiClient.put<EnergyBookingSlotDto>(`/api/v1/slots/${id}`, request)
  return toBatterySlot(data)
}

export async function activateSlot(id: string): Promise<void> {
  await apiClient.patch(`/api/v1/slots/${id}/activate`)
}

export async function deactivateSlot(id: string): Promise<void> {
  await apiClient.patch(`/api/v1/slots/${id}/deactivate`)
}
