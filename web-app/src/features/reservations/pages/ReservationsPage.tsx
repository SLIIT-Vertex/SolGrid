import { useState } from 'react'
import type { KeyboardEvent } from 'react'
import { useAuth } from '@/auth/useAuth'
import { Button } from '@/components/common/Button'
import { ReservationWorkspace } from '@/features/reservations/components/ReservationWorkspace'
import { EmptyState, ErrorState } from '@/components/common/QueryStates'
import { Pagination } from '@/components/common/Pagination'
import { cn } from '@/lib/cn'
import { ReservationFilters } from '@/features/reservations/components/ReservationFilters'
import { ReservationSummaryCards } from '@/features/reservations/components/ReservationSummaryCards'
import {
  ReservationTable,
  ReservationTableSkeleton,
} from '@/features/reservations/components/ReservationTable'
import { ReservationDetails } from '../components/ReservationDetails'
import { ReservationIcon } from '../components/ReservationIcon'
import { timezoneLabel } from '../presentation'
import { getReservationPermissions } from '../permissions'
import '../reservations.css'
import type { ReservationAction } from '../types'
import { useDashboardReservations } from '@/features/reservations/hooks/useDashboardReservations'
import { useReservationDashboardSummary } from '@/features/reservations/hooks/useReservationDashboardSummary'
import { useReservationActions } from '../hooks/useReservationActions'
import { ReservationActionDialogs } from '../components/ReservationActionDialogs'
import { defaultReservationFilters } from '@/features/reservations/types'
import type {
  Reservation,
  ReservationDashboardTab,
} from '@/features/reservations/types'

const tabs: { key: ReservationDashboardTab; label: string; compact: string }[] =
  [
    { key: 'pending', label: 'Awaiting review', compact: 'Review' },
    { key: 'current', label: 'Current reservations', compact: 'Current' },
    { key: 'history', label: 'Booking history', compact: 'History' },
  ]

export function ReservationsPage() {
  const { session } = useAuth()
  const { canManageBookings, canApprove, canReject } =
    getReservationPermissions(session?.role)
  const [editor, setEditor] = useState<{ reservation?: Reservation } | null>(
    null,
  )
  const [details, setDetails] = useState<Reservation | null>(null)
  const [tab, setTab] = useState<ReservationDashboardTab>('pending')
  const [filters, setFilters] = useState(defaultReservationFilters)

  const {
    data: summary,
    isLoading: isSummaryLoading,
    isError: isSummaryError,
  } = useReservationDashboardSummary()
  const { data, isLoading, isError, refetch } = useDashboardReservations(
    tab,
    filters,
  )
  const {
    pendingAction,
    isMutating,
    requestAction,
    dismissAction,
    confirmApprovalOrCancellation,
    confirmRejection,
  } = useReservationActions()

  const handleTabChange = (nextTab: ReservationDashboardTab) => {
    setTab(nextTab)
    setFilters((current) => ({ ...current, pageNumber: 1 }))
  }

  const handleTabKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
    const index = tabs.findIndex((item) => item.key === tab)
    let next: number
    switch (event.key) {
      case 'ArrowRight':
        next = (index + 1) % tabs.length
        break
      case 'ArrowLeft':
        next = (index + tabs.length - 1) % tabs.length
        break
      case 'Home':
        next = 0
        break
      case 'End':
        next = tabs.length - 1
        break
      default:
        return
    }
    event.preventDefault()
    handleTabChange(tabs[next].key)
    document.getElementById(`reservation-tab-${tabs[next].key}`)?.focus()
  }

  const handleAction = (
    reservation: Reservation,
    action: ReservationAction,
  ) => {
    if (
      (action === 'approve' && !canApprove) ||
      (action === 'reject' && !canReject) ||
      (action === 'cancel' && !canManageBookings)
    )
      return
    setDetails(null)
    requestAction(reservation, action)
  }

  const hasFilters = Boolean(
    filters.searchText ||
    filters.scheduledFrom ||
    filters.scheduledTo ||
    filters.status,
  )

  if (editor && canManageBookings)
    return (
      <ReservationWorkspace
        reservation={editor.reservation}
        onClose={() => setEditor(null)}
      />
    )

  return (
    <div className="reservation-workspace mx-auto max-w-7xl">
      <div className="mb-7 flex flex-wrap items-center justify-between gap-5">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink-900">
            Reservations
          </h1>
          <p className="mt-2 text-sm leading-6 text-ink-600">
            Keep every energy booking moving, from request to completion.
          </p>
        </div>
        {canManageBookings && (
          <Button
            className="reservation-primary"
            leftIcon={<ReservationIcon name="plus" />}
            onClick={() => setEditor({})}
          >
            Create reservation
          </Button>
        )}
      </div>
      <ReservationSummaryCards summary={summary} isLoading={isSummaryLoading} />
      {isSummaryError && (
        <p role="status" className="mt-2 text-xs text-ink-600">
          Reservation counts are temporarily unavailable.
        </p>
      )}

      <div className="mt-7 overflow-hidden rounded-xl border border-ink-200 bg-white">
        <div className="flex flex-wrap items-center justify-between gap-x-4 border-b border-ink-200 px-5 sm:px-6">
          <div
            className="flex gap-5 overflow-x-auto"
            role="tablist"
            aria-label="Reservation queues"
          >
            {tabs.map((item) => (
              <button
                key={item.key}
                type="button"
                role="tab"
                tabIndex={tab === item.key ? 0 : -1}
                aria-selected={tab === item.key}
                aria-controls="reservation-queue"
                id={`reservation-tab-${item.key}`}
                onClick={() => handleTabChange(item.key)}
                onKeyDown={handleTabKeyDown}
                className={cn(
                  'flex shrink-0 items-center gap-2 border-b-2 px-0 py-4 text-sm font-medium transition-colors',
                  tab === item.key
                    ? 'border-brand-700 text-brand-800'
                    : 'border-transparent text-ink-600 hover:text-ink-900',
                )}
              >
                <span className="hidden sm:inline">{item.label}</span>
                <span className="sm:hidden">{item.compact}</span>
                {item.key === 'pending' && summary && (
                  <span className="rounded bg-amber-50 px-1.5 py-0.5 text-xs tabular-nums text-amber-800">
                    {summary.pendingReservationsCount}
                  </span>
                )}
              </button>
            ))}
          </div>
          <Button
            variant="ghost"
            size="sm"
            leftIcon={<ReservationIcon name="refresh" />}
            onClick={() => void refetch()}
            disabled={isLoading}
          >
            Refresh
          </Button>
        </div>

        <div className="border-b border-ink-100 p-5 sm:px-6">
          <ReservationFilters
            filters={filters}
            onChange={setFilters}
            showStatusFilter={tab === 'history'}
          />
        </div>
        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-ink-100 px-5 py-3 sm:px-6">
          <p className="text-xs text-ink-600">
            {isLoading
              ? 'Loading your queue…'
              : isError
                ? 'Queue unavailable'
                : `${data?.totalCount ?? 0} reservations`}{' '}
            · Select a row for full details
          </p>
          <span className="text-xs text-ink-600">Times in {timezoneLabel}</span>
        </div>
        <div
          id="reservation-queue"
          role="tabpanel"
          aria-labelledby={`reservation-tab-${tab}`}
        >
          <div className="h-[38rem] overflow-auto">
            {isLoading ? (
              <ReservationTableSkeleton />
            ) : isError ? (
              <ErrorState
                message="Couldn't load reservations."
                onRetry={() => refetch()}
              />
            ) : !data || data.items.length === 0 ? (
              <EmptyState
                title={
                  hasFilters
                    ? 'No matching reservations'
                    : tab === 'pending'
                      ? 'Your review queue is clear'
                      : 'No reservations here yet'
                }
                description={
                  hasFilters
                    ? 'Try a different search or clear your filters.'
                    : tab === 'pending'
                      ? 'New booking requests will appear here when they need approval.'
                      : 'Reservations will appear here as bookings move through their lifecycle.'
                }
                icon={<ReservationIcon name="calendar" className="size-5" />}
              />
            ) : (
              <ReservationTable
                reservations={data.items}
                onView={setDetails}
                onAction={handleAction}
              />
            )}
          </div>
          <Pagination
            pageNumber={isError ? 1 : (data?.pageNumber ?? 1)}
            pageSize={filters.pageSize}
            totalCount={isError ? 0 : (data?.totalCount ?? 0)}
            onPageChange={(page) =>
              setFilters((current) => ({ ...current, pageNumber: page }))
            }
          />
        </div>
      </div>
      {details && (
        <ReservationDetails
          reservation={details}
          onClose={() => setDetails(null)}
          onEdit={(reservation) => {
            setDetails(null)
            setEditor({ reservation })
          }}
          onAction={handleAction}
        />
      )}

      <ReservationActionDialogs
        pendingAction={pendingAction}
        isLoading={isMutating}
        onConfirm={confirmApprovalOrCancellation}
        onReject={confirmRejection}
        onCancel={dismissAction}
      />
    </div>
  )
}
