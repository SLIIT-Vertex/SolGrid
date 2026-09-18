import type { Reservation } from '../types'
import { dateTimeLabel } from '../presentation'
import { ReservationIcon } from './ReservationIcon'

export function ReservationActivity({
  reservation,
}: {
  reservation: Reservation
}) {
  const events = [
    { label: 'Reservation created', at: reservation.createdAt, actor: null },
    {
      label: 'Approved',
      at: reservation.approvedAt,
      actor: reservation.approvedBy,
    },
    {
      label: 'Rejected',
      at: reservation.rejectedAt,
      actor: reservation.rejectedBy,
    },
    { label: 'Cancelled', at: reservation.cancelledAt, actor: null },
    { label: 'QR verified', at: reservation.qrVerifiedAt, actor: null },
    {
      label: 'Completed',
      at: reservation.completedAt,
      actor: reservation.completedBy,
    },
  ]
    .filter(
      (event): event is { label: string; at: string; actor: string | null } =>
        !!event.at,
    )
    .sort((a, b) => new Date(a.at).getTime() - new Date(b.at).getTime())

  return (
    <section className="border-b border-ink-100 py-7">
      <h3 className="mb-4 text-sm font-semibold text-ink-900">Activity</h3>
      <ol className="space-y-5">
        {events.map((event, index) => (
          <li key={`${event.label}-${event.at}`} className="flex gap-3">
            <div className="flex flex-col items-center">
              <span className="mt-1 flex size-5 shrink-0 items-center justify-center rounded-full bg-ink-100 text-ink-600">
                <ReservationIcon name="check" className="size-3" />
              </span>
              {index < events.length - 1 && (
                <span className="mt-2 min-h-6 w-px flex-1 bg-ink-200" />
              )}
            </div>
            <div className="min-w-0">
              <p className="text-sm font-medium text-ink-900">{event.label}</p>
              <p className="mt-1 text-xs leading-5 text-ink-600">
                {dateTimeLabel(event.at)}
              </p>
              {event.actor && (
                <p className="mt-1 break-all text-xs text-ink-600">
                  By {event.actor}
                </p>
              )}
            </div>
          </li>
        ))}
      </ol>
    </section>
  )
}
