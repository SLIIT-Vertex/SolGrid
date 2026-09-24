import { stationStatusFromValue } from '@/features/microgrid/types'
import type { StationStatus } from '@/features/microgrid/types'

export type RoutineOutcome = 'Clear' | 'Attention' | 'Breach'

const outcomeByValue: Record<number, RoutineOutcome> = {
  1: 'Clear',
  2: 'Attention',
  3: 'Breach',
}

export function routineOutcomeFromValue(value: number): RoutineOutcome {
  return outcomeByValue[value] ?? 'Attention'
}

export interface NetworkAnalytics {
  nodeCount: number
  activeNodes: number
  inactiveNodes: number
  nodesWithOpenSlots: number
  totalCapacityKw: number
  totalBatteryKwh: number
  committedBatteryKwh: number
  totalSlots: number
  availableSlots: number
  reservedSlots: number
  occupiedSlots: number
  outOfServiceSlots: number
}

export interface ReservationAnalytics {
  total: number
  pending: number
  approved: number
  rejected: number
  cancelled: number
  completed: number
  approvedFuture: number
  current: number
  history: number
  insideSevenDayWindow: number
  beyondSevenDayWindow: number
  changeable: number
  lockedByNotice: number
  pastStillOpen: number
  approvedAwaitingQr: number
  qrLive: number
  qrExpired: number
  qrVerified: number
}

export interface NodeAnalytics {
  id: string
  code: string
  name: string
  addressLine: string
  latitude: number
  longitude: number
  capacityKw: number
  status: StationStatus
  scheduleDayCount: number
  totalSlots: number
  availableSlots: number
  reservedSlots: number
  occupiedSlots: number
  outOfServiceSlots: number
  totalBatteryKwh: number
  committedBatteryKwh: number
  pendingReservations: number
  approvedReservations: number
  rejectedReservations: number
  cancelledReservations: number
  completedReservations: number
  activeReservations: number
  deactivationBlocked: boolean
  slotStateDrift: number
  utilizationPercent: number
}

export interface BusinessRoutine {
  code: string
  name: string
  rule: string
  outcome: RoutineOutcome
  measuredCount: number
  detail: string
}

export interface AccountAnalytics {
  webUsers: number
  activeWebUsers: number
  backofficeUsers: number
  gridOperatorUsers: number
  prosumers: number
  pendingProsumers: number
  activeProsumers: number
  deactivationRequested: number
  deactivatedProsumers: number
}

export interface AnalyticsSnapshot {
  generatedAtUtc: string
  network: NetworkAnalytics
  reservations: ReservationAnalytics
  nodes: NodeAnalytics[]
  routines: BusinessRoutine[]
  accounts: AccountAnalytics | null
}

export interface NodeAnalyticsDto extends Omit<NodeAnalytics, 'status'> {
  status: number
}

export interface BusinessRoutineDto extends Omit<BusinessRoutine, 'outcome'> {
  outcome: number
}

export interface AnalyticsSnapshotDto {
  generatedAtUtc: string
  network: NetworkAnalytics
  reservations: ReservationAnalytics
  nodes: NodeAnalyticsDto[]
  routines: BusinessRoutineDto[]
  accounts: AccountAnalytics | null
}

export function toAnalyticsSnapshot(dto: AnalyticsSnapshotDto): AnalyticsSnapshot {
  return {
    generatedAtUtc: dto.generatedAtUtc,
    network: dto.network,
    reservations: dto.reservations,
    nodes: dto.nodes.map((node) => ({
      ...node,
      status: stationStatusFromValue(node.status),
    })),
    routines: dto.routines.map((routine) => ({
      ...routine,
      outcome: routineOutcomeFromValue(routine.outcome),
    })),
    accounts: dto.accounts,
  }
}
