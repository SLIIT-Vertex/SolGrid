import { useEffect, useId, useRef } from 'react'
import type { ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { useQuery } from '@tanstack/react-query'
import { useAuth } from '@/auth/useAuth'
import { getReservationPermissions } from '../permissions'
import { Button } from '@/components/common/Button'
import { ErrorState } from '@/components/common/QueryStates'
import { getReservation } from '../api'
import { reservationsKeys } from '../queryKeys'
import { canChangeReservation } from '../bookingRules'
import { dateTimeLabel, timezoneLabel } from '../presentation'
import { useReservationReferences } from '../hooks/useReservationReferences'
import type { Reservation, ReservationAction } from '../types'
import { ReservationStatusBadge } from './ReservationStatusBadge'
import { ReservationIcon } from './ReservationIcon'
import { ProsumerIdentity } from './ProsumerIdentity'
import { ReservationActivity } from './ReservationActivity'

interface ReservationDetailsProps {
  reservation: Reservation
  onClose: () => void
  onEdit: (reservation: Reservation) => void
  onAction: (reservation: Reservation, action: ReservationAction) => void
}

export function ReservationDetails({
  reservation,
  onClose,
  onEdit,
  onAction,
}: ReservationDetailsProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const titleId = useId()
  useEffect(() => {
    const dialog = dialogRef.current
    const previous =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null
    const overflow = document.body.style.overflow
    dialog?.showModal()
    document.body.style.overflow = 'hidden'
    return () => {
      dialog?.close()
      document.body.style.overflow = overflow
      previous?.focus()
    }
  }, [])
  const record = useQuery({
    queryKey: reservationsKeys.detail(reservation.id),
    queryFn: () => getReservation(reservation.id),
  })

  return createPortal(
    <dialog
      ref={dialogRef}
      aria-labelledby={titleId}
      className="reservation-detail-dialog reservation-workspace"
      onCancel={(event) => {
        event.preventDefault()
        onClose()
      }}
      onClick={(event) => {
        if (event.target === event.currentTarget) {
          const box = event.currentTarget.getBoundingClientRect()
          if (
            event.clientX < box.left ||
            event.clientX > box.right ||
            event.clientY < box.top ||
            event.clientY > box.bottom
          )
            onClose()
        }
      }}
    >
      <div className="flex h-full flex-col">
        <header className="flex shrink-0 items-start justify-between gap-4 border-b border-ink-200 px-6 py-5 sm:px-8">
          <div>
            <h2 id={titleId} className="text-lg font-semibold text-ink-900">
              Reservation details
            </h2>
            <p className="mt-1 text-xs text-ink-600">
              {reservation.referenceCode}
            </p>
          </div>
          <Button
            variant="ghost"
            aria-label="Close reservation details"
            onClick={onClose}
            className="-mr-2 px-2"
          >
            <ReservationIcon name="close" className="size-5" />
          </Button>
        </header>
        {record.isPending ? (
          <div role="status" className="space-y-6 p-8">
            <span className="sr-only">Loading reservation details…</span>
            {[1, 2, 3].map((item) => (
              <div
                key={item}
                className="h-28 rounded-lg bg-ink-100 motion-safe:animate-pulse"
              />
            ))}
          </div>
        ) : record.isError ? (
          <ErrorState
            message="Reservation details couldn’t load. Try again to view the latest record."
            onRetry={() => void record.refetch()}
          />
        ) : (
          <ReservationRecord
            reservation={record.data}
            onEdit={onEdit}
            onAction={onAction}
          />
        )}
      </div>
    </dialog>,
    document.body,
  )
}

function ReservationRecord({
  reservation,
  onEdit,
  onAction,
}: Omit<ReservationDetailsProps, 'onClose'>) {
  const { prosumer, person, station, slot, canReadProsumer } =
    useReservationReferences(reservation)
  const { session } = useAuth()
  const { canManageBookings, canApprove, canReject } =
    getReservationPermissions(session?.role)
  const editable = canManageBookings && canChangeReservation(reservation)
  const active =
    reservation.status === 'Pending' || reservation.status === 'Approved'

  return (
    <>
      <div className="min-h-0 flex-1 overflow-y-auto px-6 py-6 sm:px-8">
        <div className="mb-7 flex flex-wrap items-center justify-between gap-3">
          <ReservationStatusBadge status={reservation.status} />
          <span className="text-xs text-ink-600">
            Last updated {dateTimeLabel(reservation.updatedAt)}
          </span>
        </div>
        <section className="border-b border-ink-100 pb-7">
          <h3 className="mb-4 text-sm font-semibold text-ink-900">Prosumer</h3>
          {person ? (
            <>
              <ProsumerIdentity prosumer={person} />
              <dl className="mt-5 grid grid-cols-2 gap-4">
                <DetailField label="Account status">
                  {person.status}
                </DetailField>
                <DetailField label="Phone number">
                  {person.phoneNumber ? (
                    <a
                      href={`tel:${person.phoneNumber}`}
                      className="hover:underline"
                    >
                      {person.phoneNumber}
                    </a>
                  ) : (
                    'Not provided'
                  )}
                </DetailField>
                <DetailField label="Email" wide>
                  <a
                    href={`mailto:${person.email}`}
                    className="text-brand-800 hover:underline"
                  >
                    {person.email}
                  </a>
                </DetailField>
                <DetailField label="Registered">
                  {dateTimeLabel(person.createdAt)}
                </DetailField>
                <DetailField label="Profile updated">
                  {dateTimeLabel(person.updatedAt)}
                </DetailField>
              </dl>
            </>
          ) : (
            <>
              <p className="text-sm font-medium text-ink-900">
                NIC {reservation.prosumerId}
              </p>
              {!canReadProsumer ? (
                <p className="mt-2 text-xs leading-5 text-ink-600">
                  Prosumer profile details are unavailable for this reservation.
                </p>
              ) : prosumer.isPending ? (
                <p role="status" className="mt-2 text-sm text-ink-600">
                  Loading prosumer profile…
                </p>
              ) : (
                <ReferenceError
                  label="Prosumer profile couldn’t load."
                  onRetry={() => void prosumer.refetch()}
                />
              )}
            </>
          )}
        </section>
        <section className="border-b border-ink-100 py-7">
          <h3 className="mb-4 text-sm font-semibold text-ink-900">
            Reservation & energy slot
          </h3>
          <dl className="grid grid-cols-2 gap-x-5 gap-y-5">
            <DetailField label="Scheduled arrival" wide>
              <span className="text-base font-semibold">
                {dateTimeLabel(reservation.scheduledAt)}
              </span>
              <span className="mt-1 block text-xs font-normal text-ink-600">
                {timezoneLabel}
              </span>
            </DetailField>
            <DetailField label="Grid node">
              {station.data?.name ?? reservation.stationId}
            </DetailField>
            <DetailField label="Station code">
              {station.data?.code ?? '—'}
            </DetailField>
            <DetailField label="Address" wide>
              {station.data?.addressLine ?? '—'}
            </DetailField>
            <DetailField label="Battery slot">
              {slot.data
                ? `Slot ${slot.data.slotNumber}`
                : reservation.bookingSlotId}
            </DetailField>
            <DetailField label="Battery capacity">
              {slot.data ? `${slot.data.batteryCapacityKwh} kWh` : '—'}
            </DetailField>
            <DetailField label="Slot time window" wide>
              {slot.data
                ? `${dateTimeLabel(slot.data.startTime)} – ${dateTimeLabel(slot.data.endTime)}`
                : '—'}
            </DetailField>
            <DetailField label="Station capacity">
              {station.data ? `${station.data.capacityKw} kW` : '—'}
            </DetailField>
            <DetailField label="Station status">
              {station.data?.status ?? '—'}
            </DetailField>
          </dl>
          {station.data && (
            <a
              href={`https://www.google.com/maps/search/?api=1&query=${station.data.latitude},${station.data.longitude}`}
              target="_blank"
              rel="noreferrer"
              className="mt-4 inline-flex items-center gap-2 text-sm font-medium text-brand-800 hover:underline"
            >
              View station location
              <ReservationIcon name="external" />
            </a>
          )}
          {station.isError && (
            <ReferenceError
              label="Station details couldn’t load."
              onRetry={() => void station.refetch()}
            />
          )}
          {slot.isError && (
            <ReferenceError
              label="Battery slot details couldn’t load."
              onRetry={() => void slot.refetch()}
            />
          )}
        </section>
        {reservation.rejectionReason && (
          <section className="border-b border-ink-100 py-7">
            <h3 className="text-sm font-semibold text-ink-900">
              Rejection reason
            </h3>
            <p className="mt-3 whitespace-pre-wrap break-words text-sm leading-6 text-red-700">
              {reservation.rejectionReason}
            </p>
          </section>
        )}
        <ReservationActivity reservation={reservation} />
        <section className="border-b border-ink-100 py-7">
          <h3 className="mb-4 text-sm font-semibold text-ink-900">
            QR verification
          </h3>
          <dl className="grid grid-cols-2 gap-4">
            <DetailField label="Verification status">
              {reservation.qrVerifiedAt
                ? 'Verified'
                : reservation.hasQrVerificationToken
                  ? 'Token issued'
                  : 'Not issued'}
            </DetailField>
            <DetailField label="Verified at">
              {reservation.qrVerifiedAt
                ? dateTimeLabel(reservation.qrVerifiedAt)
                : 'Not verified'}
            </DetailField>
            <DetailField label="Token issued">
              {reservation.qrVerificationTokenIssuedAt
                ? dateTimeLabel(reservation.qrVerificationTokenIssuedAt)
                : '—'}
            </DetailField>
            <DetailField label="Token expires">
              {reservation.qrVerificationTokenExpiresAt
                ? dateTimeLabel(reservation.qrVerificationTokenExpiresAt)
                : '—'}
            </DetailField>
          </dl>
        </section>
        <details className="py-7">
          <summary className="cursor-pointer text-sm font-medium text-ink-700">
            Record identifiers
          </summary>
          <dl className="mt-4 space-y-4">
            <DetailField label="Reservation ID">{reservation.id}</DetailField>
            <DetailField label="Prosumer NIC">
              {reservation.prosumerId}
            </DetailField>
            <DetailField label="Station ID">
              {reservation.stationId}
            </DetailField>
            <DetailField label="Booking slot ID">
              {reservation.bookingSlotId}
            </DetailField>
          </dl>
        </details>
      </div>
      <footer className="shrink-0 border-t border-ink-200 bg-white px-6 py-5 sm:px-8">
        {canManageBookings && active && !editable && (
          <p className="mb-4 text-xs leading-5 text-ink-600">
            Editing and cancellation are unavailable within twelve hours of the
            scheduled time.
          </p>
        )}
        <div className="flex flex-wrap items-center gap-2">
          {active && !canManageBookings && !canApprove && !canReject && (
            <p className="text-sm text-ink-600">View-only access</p>
          )}
          {reservation.status === 'Pending' && (
            <>
              {canApprove && (
                <Button
                  className="reservation-primary"
                  onClick={() => onAction(reservation, 'approve')}
                  leftIcon={<ReservationIcon name="check" />}
                >
                  Approve reservation
                </Button>
              )}
              {canReject && (
                <Button
                  variant="danger"
                  onClick={() => onAction(reservation, 'reject')}
                >
                  Reject
                </Button>
              )}
            </>
          )}
          {editable && (
            <>
              <Button variant="secondary" onClick={() => onEdit(reservation)}>
                Edit reservation
              </Button>
              <Button
                variant="ghost"
                onClick={() => onAction(reservation, 'cancel')}
              >
                Cancel reservation
              </Button>
            </>
          )}
          {!active && (
            <p className="text-sm text-ink-600">
              This reservation is {reservation.status.toLowerCase()}.
            </p>
          )}
        </div>
      </footer>
    </>
  )
}

function DetailField({
  label,
  children,
  wide,
}: {
  label: string
  children: ReactNode
  wide?: boolean
}) {
  return (
    <div className={wide ? 'col-span-2 min-w-0' : 'min-w-0'}>
      <dt className="text-xs text-ink-600">{label}</dt>
      <dd className="mt-1.5 break-words text-sm text-ink-900 [overflow-wrap:anywhere]">
        {children}
      </dd>
    </div>
  )
}
function ReferenceError({
  label,
  onRetry,
}: {
  label: string
  onRetry: () => void
}) {
  return (
    <p role="alert" className="mt-3 text-xs text-red-700">
      {label}{' '}
      <button
        type="button"
        className="underline underline-offset-4"
        onClick={onRetry}
      >
        Try again
      </button>
    </p>
  )
}
