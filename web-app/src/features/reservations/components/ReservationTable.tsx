import { Button } from '@/components/common/Button'
import { useAuth } from '@/auth/useAuth'
import { getReservationPermissions } from '../permissions'
import { useReservationReferences } from '../hooks/useReservationReferences'
import {
  dateLabel,
  dateTimeLabel,
  timeLabel,
} from '../presentation'
import { ReservationStatusBadge } from './ReservationStatusBadge'
import { ReservationIcon } from './ReservationIcon'
import type { Reservation, ReservationAction } from '../types'

interface ReservationTableProps {
  reservations: Reservation[]
  onView: (reservation: Reservation) => void
  onAction: (reservation: Reservation, action: ReservationAction) => void
}

export function ReservationTable({
  reservations,
  onAction,
  onView,
}: ReservationTableProps) {
  const { session } = useAuth()
  const { canApprove } = getReservationPermissions(session?.role)
  return (
    <>
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full min-w-[850px] text-left text-sm">
          <caption className="sr-only">
            Energy reservations. Select a reservation reference or row to view
            complete details.
          </caption>
          <thead>
            <tr className="border-b border-ink-200 bg-ink-50/70 text-xs text-ink-600">
              {[
                'Reservation / Prosumer',
                'Grid node',
                'Scheduled arrival',
                'Status',
                'Review',
              ].map((label) => (
                <th
                  key={label}
                  scope="col"
                  className="px-6 py-3.5 font-medium last:text-right"
                >
                  {label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-ink-100">
            {reservations.map((reservation) => (
              <ReservationRow
                key={reservation.id}
                reservation={reservation}
                canApprove={canApprove}
                onAction={onAction}
                onView={onView}
              />
            ))}
          </tbody>
        </table>
      </div>
      <div className="divide-y divide-ink-100 md:hidden">
        {reservations.map((reservation) => (
          <ReservationMobileRow
            key={reservation.id}
            reservation={reservation}
            onView={onView}
          />
        ))}
      </div>
    </>
  )
}

function ReservationRow({
  reservation,
  canApprove,
  onAction,
  onView,
}: Omit<ReservationTableProps, 'reservations'> & {
  reservation: Reservation
  canApprove: boolean
}) {
  const { prosumer, person, station, slot, canReadProsumer } =
    useReservationReferences(reservation)
  return (
    <tr
      onClick={() => onView(reservation)}
      className="cursor-pointer transition-colors hover:bg-ink-50/80 focus-within:bg-ink-50"
    >
      <td className="px-6 py-5">
        <button
          type="button"
          onClick={(event) => {
            event.stopPropagation()
            onView(reservation)
          }}
          className="mb-3 text-xs font-medium text-brand-800 underline-offset-4 hover:underline"
        >
          {reservation.referenceCode}
        </button>
        <div className="flex items-center gap-3">
          <span
            aria-hidden="true"
            className="flex size-9 shrink-0 items-center justify-center rounded-full bg-ink-100 text-xs font-semibold text-ink-700"
          >
            {person ? `${person.firstName[0]}${person.lastName[0]}` : 'P'}
          </span>
          <div>
            <p className="font-medium text-ink-900">
              {person
                ? `${person.firstName} ${person.lastName}`
                : canReadProsumer && prosumer.isPending
                  ? 'Loading prosumer…'
                  : reservation.prosumerId}
            </p>
            <p className="mt-1 text-xs text-ink-600">
              {person
                ? person.nic
                : canReadProsumer && prosumer.isError
                  ? 'Profile unavailable'
                  : 'Prosumer NIC'}
            </p>
          </div>
        </div>
      </td>
      <td className="max-w-64 px-6 py-5">
        <p className="font-medium text-ink-900">
          {station.data?.name ??
            (station.isPending ? 'Loading station…' : reservation.stationId)}
        </p>
        <p className="mt-1.5 text-xs text-ink-600">
          {station.data?.code ?? 'Station'}
          {slot.data &&
            ` · Slot ${slot.data.slotNumber} · ${slot.data.batteryCapacityKwh} kWh`}
        </p>
        <p
          className="mt-1.5 truncate text-xs text-ink-600"
          title={station.data?.addressLine}
        >
          {station.data?.addressLine}
        </p>
      </td>
      <td className="whitespace-nowrap px-6 py-5 tabular-nums">
        <p className="font-medium text-ink-900">
          {dateLabel(reservation.scheduledAt)}
        </p>
        <p className="mt-1.5 flex items-center gap-1.5 text-xs text-ink-600">
          <ReservationIcon name="clock" className="size-3.5" />
          {timeLabel(reservation.scheduledAt)}
        </p>
      </td>
      <td className="px-6 py-5">
        <ReservationStatusBadge status={reservation.status} />
        <p className="mt-2 text-xs text-ink-600">
          Created {dateLabel(reservation.createdAt)}
        </p>
      </td>
      <td className="px-6 py-5">
        <div className="flex items-center justify-end gap-3">
          {canApprove && reservation.status === 'Pending' && (
            <Button
              size="sm"
              variant="secondary"
              className="border-brand-200 text-brand-800 hover:bg-brand-50"
              onClick={(event) => {
                event.stopPropagation()
                onAction(reservation, 'approve')
              }}
            >
              Approve
            </Button>
          )}
          <button
            type="button"
            aria-label={`View ${reservation.referenceCode}`}
            onClick={(event) => {
              event.stopPropagation()
              onView(reservation)
            }}
            className="rounded-lg p-2 text-ink-600 hover:bg-ink-100"
          >
            <ReservationIcon name="chevron" />
          </button>
        </div>
      </td>
    </tr>
  )
}

function ReservationMobileRow({
  reservation,
  onView,
}: {
  reservation: Reservation
  onView: (reservation: Reservation) => void
}) {
  const { person, station, slot } = useReservationReferences(reservation)
  return (
    <button
      type="button"
      onClick={() => onView(reservation)}
      className="block w-full px-5 py-5 text-left hover:bg-ink-50"
    >
      <div className="mb-3 flex items-center justify-between gap-3">
        <span className="text-xs font-medium text-brand-800">
          {reservation.referenceCode}
        </span>
        <ReservationStatusBadge status={reservation.status} />
      </div>
      <p className="text-sm font-semibold text-ink-900">
        {person
          ? `${person.firstName} ${person.lastName}`
          : reservation.prosumerId}
      </p>
      <p className="mt-2 text-sm text-ink-700">
        {station.data?.name ?? reservation.stationId}
      </p>
      <p className="mt-1 text-xs text-ink-600">
        {slot.data
          ? `Slot ${slot.data.slotNumber} · ${slot.data.batteryCapacityKwh} kWh`
          : 'Energy reservation'}
      </p>
      <div className="mt-4 flex items-center justify-between gap-3 border-t border-ink-100 pt-3">
        <span className="text-xs tabular-nums text-ink-700">
          {dateTimeLabel(reservation.scheduledAt)}
        </span>
        <ReservationIcon
          name="chevron"
          className="size-4 shrink-0 text-ink-600"
        />
      </div>
    </button>
  )
}

export function ReservationTableSkeleton() {
  return (
    <div
      role="status"
      aria-label="Loading reservations"
      className="divide-y divide-ink-100"
    >
      <span className="sr-only">Loading reservations…</span>
      {[1, 2, 3, 4, 5].map((row) => (
        <div key={row} className="flex gap-8 px-6 py-6">
          <div className="h-12 flex-1 rounded bg-ink-100 motion-safe:animate-pulse" />
          <div className="hidden h-12 flex-1 rounded bg-ink-100 motion-safe:animate-pulse sm:block" />
          <div className="h-12 w-24 rounded bg-ink-100 motion-safe:animate-pulse" />
        </div>
      ))}
    </div>
  )
}
