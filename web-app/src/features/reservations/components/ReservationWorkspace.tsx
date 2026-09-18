import { useRef } from 'react'
import { Button } from '@/components/common/Button'
import type { Reservation } from '../types'
import { useReservationBooking } from '../hooks/useReservationBooking'
import { getErrorMessage } from '@/lib/problemDetails'
import { cn } from '@/lib/cn'
import { dateTimeLabel, timezoneLabel } from '../presentation'
import { ProsumerPicker } from './ProsumerPicker'
import {
  GridNodeStep,
  SlotTimeStep,
  ReviewStep,
} from './ReservationBookingSteps'
import { ReservationIcon } from './ReservationIcon'
import { bookingStepOrder, bookingSteps } from '../bookingSteps'

export function ReservationWorkspace({
  reservation,
  onClose,
}: {
  reservation?: Reservation
  onClose: () => void
}) {
  const headingRef = useRef<HTMLHeadingElement>(null)
  const {
    openedAt,
    step,
    person: selectedPerson,
    setPerson,
    bookingSlotId,
    scheduledAt,
    changeScheduledAt,
    selectSlot,
    validation,
    stations,
    selectedStation,
    slots,
    sortedSlots,
    selectedSlot,
    editable,
    stationActive,
    selectStation,
    save,
    goTo,
    submit,
    canContinue,
  } = useReservationBooking(reservation, onClose, () =>
    headingRef.current?.focus(),
  )
  const stepIndex = bookingStepOrder.indexOf(step)
  const currentStep = bookingSteps[step]
  const firstStep = reservation ? 'grid-node' : 'prosumer'
  const canGoBack = step !== firstStep

  return (
    <div className="reservation-workspace mx-auto max-w-7xl">
      <Button
        variant="ghost"
        className="mb-5 -ml-3"
        leftIcon={<ReservationIcon name="back" />}
        disabled={save.isPending}
        onClick={onClose}
      >
        Back to reservations
      </Button>
      <div className="mb-7 flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink-900">
            {reservation ? 'Edit reservation' : 'Create a reservation'}
          </h1>
          <p className="mt-2 text-sm leading-6 text-ink-600">
            Connect the right prosumer with the right energy slot.
          </p>
        </div>
        <span className="rounded-lg border border-ink-200 bg-white px-3 py-2 text-xs text-ink-600">
          Step {stepIndex + 1} of 4
        </span>
      </div>
      <ol
        aria-label="Reservation steps"
        className="reservation-stepper mb-7 grid grid-cols-4 overflow-hidden rounded-xl border border-ink-200 bg-white"
      >
        {bookingStepOrder.map((id, index) => (
          <li
            key={id}
            className={cn(
              'border-r border-ink-100 last:border-0',
              step === id && 'bg-brand-50/70',
            )}
          >
            <button
              type="button"
              aria-current={step === id ? 'step' : undefined}
              disabled={
                index > stepIndex ||
                save.isPending ||
                (!!reservation && index === 0)
              }
              onClick={() => goTo(id)}
              className="flex w-full items-center gap-3 px-5 py-4 text-left disabled:cursor-default"
            >
              <span
                className={cn(
                  'flex size-7 shrink-0 items-center justify-center rounded-full text-xs font-semibold',
                  stepIndex >= index
                    ? 'bg-brand-700 text-white'
                    : 'bg-ink-100 text-ink-600',
                )}
              >
                {index < stepIndex ? (
                  <ReservationIcon name="check" className="size-4" />
                ) : (
                  index + 1
                )}
              </span>
              <span
                className={cn(
                  'text-sm font-medium',
                  step === id ? 'text-brand-900' : 'text-ink-600',
                )}
              >
                {bookingSteps[id].label}
              </span>
            </button>
          </li>
        ))}
      </ol>
      <form onSubmit={submit} noValidate>
        <fieldset disabled={save.isPending}>
          <legend className="sr-only">Reservation booking details</legend>
          <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_280px]">
            <section className="overflow-hidden rounded-xl border border-ink-200 bg-white">
              <div className="border-b border-ink-100 px-6 py-5 sm:px-7">
                <h2
                  ref={headingRef}
                  tabIndex={-1}
                  className="text-lg font-semibold text-ink-900 outline-none"
                >
                  {currentStep.title}
                </h2>
                <p className="mt-1.5 text-sm leading-6 text-ink-600">
                  {currentStep.description}
                </p>
              </div>
              <div className="min-h-80 p-6 sm:p-7">
                {step === 'prosumer' && (
                  <ProsumerPicker
                    selected={selectedPerson}
                    onSelect={setPerson}
                  />
                )}
                {step === 'grid-node' && (
                  <GridNodeStep
                    nodes={stations.data ?? []}
                    selectedStation={selectedStation}
                    loading={stations.isPending}
                    error={stations.isError}
                    onRetry={() => void stations.refetch()}
                    onSelect={selectStation}
                  />
                )}
                {step === 'slot-time' && (
                  <SlotTimeStep
                    sortedSlots={sortedSlots}
                    selectedStation={selectedStation}
                    selectedSlot={selectedSlot}
                    bookingSlotId={bookingSlotId}
                    currentSlotId={reservation?.bookingSlotId}
                    scheduledAt={scheduledAt}
                    openedAt={openedAt}
                    loading={slots.isPending}
                    error={slots.isError}
                    onRetry={() => void slots.refetch()}
                    onChangeStation={() => goTo('grid-node')}
                    onSelectSlot={selectSlot}
                    onTimeChange={changeScheduledAt}
                  />
                )}
                {step === 'review' && (
                  <ReviewStep
                    selectedPerson={selectedPerson}
                    prosumerId={reservation?.prosumerId}
                    selectedStation={selectedStation}
                    selectedSlot={selectedSlot}
                    scheduledAt={scheduledAt}
                    isEditing={!!reservation}
                    goTo={goTo}
                  />
                )}
              </div>
              {!editable && (
                <p role="alert" className="px-7 pb-5 text-sm text-red-700">
                  This reservation cannot be changed. Updates require an active
                  reservation and at least twelve hours’ notice.
                </p>
              )}
              {(validation || save.isError) && (
                <p role="alert" className="px-7 pb-5 text-sm text-red-700">
                  {validation ?? getErrorMessage(save.error)}
                </p>
              )}
              <div className="flex flex-wrap items-center justify-between gap-3 border-t border-ink-100 bg-ink-50/60 px-6 py-5 sm:px-7">
                <Button
                  type="button"
                  variant="secondary"
                  disabled={save.isPending}
                  onClick={() =>
                    canGoBack
                      ? goTo(currentStep.previous ?? firstStep)
                      : onClose()
                  }
                >
                  {canGoBack ? 'Previous step' : 'Cancel'}
                </Button>
                <Button
                  type="submit"
                  className="reservation-primary"
                  disabled={
                    save.isPending ||
                    !canContinue ||
                    (!stationActive && step !== 'prosumer')
                  }
                  isLoading={save.isPending}
                  leftIcon={
                    step !== 'review' ? (
                      <ReservationIcon name="arrow" />
                    ) : (
                      <ReservationIcon name="check" />
                    )
                  }
                >
                  {step !== 'review'
                    ? currentStep.nextLabel
                    : reservation
                      ? 'Save changes'
                      : 'Create reservation'}
                </Button>
              </div>
            </section>
            <aside className="reservation-booking-summary rounded-xl border border-ink-200 bg-white p-5 xl:sticky xl:top-6">
              <h2 className="text-sm font-semibold text-ink-900">
                Your reservation
              </h2>
              <p className="mt-1 text-xs leading-5 text-ink-600">
                Your selections stay here as you go.
              </p>
              <dl className="mt-5 space-y-5">
                <SummaryItem
                  label="Prosumer"
                  value={
                    selectedPerson
                      ? `${selectedPerson.firstName} ${selectedPerson.lastName}`
                      : reservation?.prosumerId
                  }
                  secondary={selectedPerson?.nic}
                />
                <SummaryItem
                  label="Grid node"
                  value={selectedStation?.name}
                  secondary={selectedStation?.code}
                />
                <SummaryItem
                  label="Battery slot"
                  value={
                    selectedSlot
                      ? `Slot ${selectedSlot.slotNumber} · ${selectedSlot.batteryCapacityKwh} kWh`
                      : undefined
                  }
                />
                <SummaryItem
                  label="Scheduled arrival"
                  value={scheduledAt ? dateTimeLabel(scheduledAt) : undefined}
                />
              </dl>
              <div className="mt-6 border-t border-ink-100 pt-5">
                <div className="flex items-start gap-2 text-xs leading-5 text-ink-600">
                  <ReservationIcon
                    name="clock"
                    className="mt-0.5 size-4 shrink-0"
                  />
                  <p>
                    Book up to seven days ahead.
                    <br />
                    All times use {timezoneLabel}.
                  </p>
                </div>
              </div>
            </aside>
          </div>
        </fieldset>
      </form>
    </div>
  )
}

function SummaryItem({
  label,
  value,
  secondary,
}: {
  label: string
  value?: string
  secondary?: string
}) {
  return (
    <div>
      <dt className="text-xs text-ink-600">{label}</dt>
      <dd
        className={cn(
          'mt-1 break-words text-sm',
          value ? 'font-medium text-ink-900' : 'text-ink-600',
        )}
      >
        {value ?? 'Not selected yet'}
      </dd>
      {secondary && (
        <dd className="mt-1 break-all text-xs text-ink-600">{secondary}</dd>
      )}
    </div>
  )
}
