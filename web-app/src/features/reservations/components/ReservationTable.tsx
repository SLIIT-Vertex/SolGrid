import { Button } from '@/components/common/Button'
import { ReservationStatusBadge } from '@/features/reservations/components/ReservationStatusBadge'
import type { Reservation } from '@/features/reservations/types'

export type ReservationAction = 'approve' | 'reject' | 'cancel'

interface ReservationTableProps {
  reservations: Reservation[]
  onEdit?: (reservation: Reservation) => void
  onAction: (reservation: Reservation, action: ReservationAction) => void
}

const dateTimeFormatter = new Intl.DateTimeFormat('en-GB', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

export function ReservationTable({ reservations, onAction, onEdit }: ReservationTableProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[820px] text-left text-sm">
        <thead>
          <tr className="border-b border-ink-100 text-xs uppercase tracking-wide text-ink-400">
            <th className="px-4 py-3 font-medium">Prosumer</th>
            <th className="px-4 py-3 font-medium">Station</th>
            <th className="px-4 py-3 font-medium">Scheduled</th>
            <th className="px-4 py-3 font-medium">Status</th>
            <th className="px-4 py-3 font-medium">Created</th>
            <th className="px-4 py-3 font-medium text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {reservations.map((reservation) => (
            <tr key={reservation.id} className="hover:bg-ink-50/60">
              <td className="px-4 py-3 font-medium text-ink-900">{reservation.prosumerId}</td>
              <td className="px-4 py-3 text-ink-600">{reservation.stationId}</td>
              <td className="px-4 py-3 text-ink-600">{dateTimeFormatter.format(new Date(reservation.scheduledAt))}</td>
              <td className="px-4 py-3">
                <ReservationStatusBadge status={reservation.status} />
              </td>
              <td className="px-4 py-3 text-ink-500">{dateTimeFormatter.format(new Date(reservation.createdAt))}</td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-2">
                  {onEdit && (reservation.status === 'Pending' || reservation.status === 'Approved') && <Button variant="secondary" size="sm" onClick={() => onEdit(reservation)}>Edit</Button>}
                  {reservation.status === 'Pending' ? (
                    <>
                      <Button size="sm" onClick={() => onAction(reservation, 'approve')}>
                        Approve
                      </Button>
                      <Button variant="danger" size="sm" onClick={() => onAction(reservation, 'reject')}>
                        Reject
                      </Button>
                    </>
                  ) : null}
                  {reservation.status === 'Pending' || reservation.status === 'Approved' ? (
                    <Button variant="secondary" size="sm" onClick={() => onAction(reservation, 'cancel')}>
                      Cancel
                    </Button>
                  ) : null}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
