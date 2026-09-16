export const MICROGRID_DETAIL_TABS = ['overview', 'schedule', 'slots'] as const

export type MicrogridDetailTab = (typeof MICROGRID_DETAIL_TABS)[number]

export type StationStatus = 'Active' | 'Inactive'

export type SlotAvailabilityFilter = '' | 'available' | 'none'

export const StationStatusValue = {
  Active: 1,
  Inactive: 2,
} as const satisfies Record<StationStatus, number>

export function stationStatusFromValue(value: number): StationStatus {
  return value === StationStatusValue.Inactive ? 'Inactive' : 'Active'
}

export type SlotStatus = 'Available' | 'Reserved' | 'Occupied' | 'OutOfService'

export const SlotStatusValue = {
  Available: 1,
  Reserved: 2,
  Occupied: 3,
  OutOfService: 4,
} as const satisfies Record<SlotStatus, number>

export function slotStatusFromValue(value: number): SlotStatus {
  if (value === SlotStatusValue.Reserved) return 'Reserved'
  if (value === SlotStatusValue.Occupied) return 'Occupied'
  if (value === SlotStatusValue.OutOfService) return 'OutOfService'
  return 'Available'
}

export const WEEKDAYS = [
  { day: 1, label: 'Monday' },
  { day: 2, label: 'Tuesday' },
  { day: 3, label: 'Wednesday' },
  { day: 4, label: 'Thursday' },
  { day: 5, label: 'Friday' },
  { day: 6, label: 'Saturday' },
  { day: 0, label: 'Sunday' },
] as const

/** Weekly windows are TimeOnly values with no IANA zone on the station API. */
export const SCHEDULE_CLOCK_NOTE = 'Times shown as weekday clock hours (no timezone).'

export const SCHEDULE_REPLACE_NOTE =
  'Saving replaces the full weekly schedule. Existing battery slots and reservations are not checked or changed.'

export const MAX_CODE_LENGTH = 32
export const MAX_NAME_LENGTH = 120
export const MAX_ADDRESS_LENGTH = 250
export const MAX_SLOTS_PER_STATION = 100

export interface GeoCoordinatesDto {
  latitude: number
  longitude: number
}

export interface OperatingWindowDto {
  day: number
  opensAt: string
  closesAt: string
}

export interface EnergyBookingSlotDto {
  id: string
  stationId: string
  slotNumber: number
  batteryCapacityKwh: number
  startTime: string
  endTime: string
  status: number
  isActive: boolean
  isAvailable: boolean
}

/** Raw station payload from GET /api/v1/stations. Enums are numbers. */
export interface SolarStationDto {
  id: string
  code: string
  name: string
  addressLine: string
  location: GeoCoordinatesDto
  capacityKw: number
  status: number
  totalSlotCount: number
  availableSlotCount: number
  slots?: EnergyBookingSlotDto[]
  schedule?: OperatingWindowDto[]
}

export interface PagedResultDto<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

export interface OperatingWindow {
  day: number
  opensAt: string
  closesAt: string
}

export interface MicrogridBatterySlot {
  id: string
  stationId: string
  slotNumber: number
  batteryCapacityKwh: number
  startTime: string
  endTime: string
  status: SlotStatus
  isActive: boolean
  isAvailable: boolean
}

export interface MicrogridNode {
  id: string
  code: string
  name: string
  addressLine: string
  latitude: number
  longitude: number
  capacityKw: number
  status: StationStatus
  totalSlotCount: number
  availableSlotCount: number
  schedule: OperatingWindow[]
  slots: MicrogridBatterySlot[]
}

export interface MicrogridNodeFilters {
  searchText: string
  status: StationStatus | ''
  hasAvailableSlots: SlotAvailabilityFilter
  pageNumber: number
  pageSize: number
}

export const defaultMicrogridNodeFilters: MicrogridNodeFilters = {
  searchText: '',
  status: '',
  hasAvailableSlots: '',
  pageNumber: 1,
  pageSize: 10,
}

export function hasMicrogridNodeFilters(filters: MicrogridNodeFilters): boolean {
  return Boolean(filters.searchText.trim() || filters.status || filters.hasAvailableSlots)
}

export interface MicrogridSlotFilters {
  status: SlotStatus | ''
  fromDate: string
  toDate: string
  pageNumber: number
  pageSize: number
}

export const defaultMicrogridSlotFilters: MicrogridSlotFilters = {
  status: '',
  fromDate: '',
  toDate: '',
  pageNumber: 1,
  pageSize: 10,
}

export function hasMicrogridSlotFilters(filters: MicrogridSlotFilters): boolean {
  return Boolean(filters.status || filters.fromDate || filters.toDate)
}

export function isCommittedSlot(status: SlotStatus): boolean {
  return status === 'Reserved' || status === 'Occupied'
}

export interface OperatingWindowRequest {
  day: number
  opensAt: string
  closesAt: string
}

export interface CreateBookingSlotRequest {
  slotNumber: number
  batteryCapacityKwh: number
  startTime: string
  endTime: string
}

export interface UpdateBookingSlotRequest {
  batteryCapacityKwh: number
  startTime: string
  endTime: string
}

export interface CreateSolarStationRequest {
  code: string
  name: string
  addressLine: string
  location: { latitude: number; longitude: number }
  capacityKw: number
  slots: CreateBookingSlotRequest[]
  schedule: OperatingWindowRequest[]
}

export interface UpdateSolarStationRequest {
  code: string
  name: string
  addressLine: string
  location: { latitude: number; longitude: number }
  capacityKw: number
}

export interface UpdateStationScheduleRequest {
  schedule: OperatingWindowRequest[]
}

export interface ScheduleFormValues {
  schedule: ScheduleWindowFormValues[]
}

export interface ScheduleWindowFormValues {
  day: number
  opensAt: string
  closesAt: string
}

export interface BatterySlotFormValues {
  slotNumber: string
  batteryCapacityKwh: string
  startDate: string
  startTime: string
  endDate: string
  endTime: string
}

export interface MicrogridNodeFormValues {
  code: string
  name: string
  addressLine: string
  latitude: string
  longitude: string
  capacityKw: string
  schedule: ScheduleWindowFormValues[]
  slots: BatterySlotFormValues[]
}

export const defaultCreateNodeValues: MicrogridNodeFormValues = {
  code: '',
  name: '',
  addressLine: '',
  latitude: '',
  longitude: '',
  capacityKw: '',
  schedule: [{ day: 1, opensAt: '08:00', closesAt: '17:00' }],
  slots: [],
}
