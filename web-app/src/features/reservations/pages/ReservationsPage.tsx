import { useState } from 'react'
import { Button } from '@/components/common/Button'
import { ReservationDialog } from '@/features/reservations/components/ReservationDialog'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { EmptyState, ErrorState, LoadingState } from '@/components/common/QueryStates'
import { Pagination } from '@/components/common/Pagination'
import { useToast } from '@/components/common/useToast'
import { cn } from '@/lib/cn'
import { RejectReservationDialog } from '@/features/reservations/components/RejectReservationDialog'
import { ReservationFilters } from '@/features/reservations/components/ReservationFilters'
import { ReservationSummaryCards } from '@/features/reservations/components/ReservationSummaryCards'
import { ReservationTable } from '@/features/reservations/components/ReservationTable'
import type { ReservationAction } from '@/features/reservations/components/ReservationTable'
import { useDashboardReservations } from '@/features/reservations/hooks/useDashboardReservations'
import { useReservationDashboardSummary } from '@/features/reservations/hooks/useReservationDashboardSummary'
import {
  useApproveReservation,
  useCancelReservation,
  useRejectReservation,
} from '@/features/reservations/hooks/useReviewReservation'
import { defaultReservationFilters } from '@/features/reservations/types'
import type { Reservation, ReservationDashboardTab } from '@/features/reservations/types'
import { getErrorMessage } from '@/lib/problemDetails'

const tabs: { key: ReservationDashboardTab; label: string }[] = [
  { key: 'current', label: 'Current' },
  { key: 'pending', label: 'Pending' },
  { key: 'history', label: 'History' },
]

export function ReservationsPage() {
  const [editor, setEditor] = useState<{ reservation?: Reservation } | null>(null)
  const [tab, setTab] = useState<ReservationDashboardTab>('pending')
  const [filters, setFilters] = useState(defaultReservationFilters)
  const [pendingAction, setPendingAction] = useState<{ reservation: Reservation; action: ReservationAction } | null>(
    null,
  )

  const { data: summary, isLoading: isSummaryLoading } = useReservationDashboardSummary()
  const { data, isLoading, isError, refetch } = useDashboardReservations(tab, filters)
  const approveReservation = useApproveReservation()
  const rejectReservation = useRejectReservation()
  const cancelReservation = useCancelReservation()
  const { showToast } = useToast()

  const isMutating = approveReservation.isPending || rejectReservation.isPending || cancelReservation.isPending

  const handleTabChange = (nextTab: ReservationDashboardTab) => {
    setTab(nextTab)
    setFilters((current) => ({ ...current, pageNumber: 1 }))
  }

  const handleAction = (reservation: Reservation, action: ReservationAction) => {
    setPendingAction({ reservation, action })
  }

  const handleApproveOrCancelConfirm = async () => {
    if (!pendingAction) return
    const { reservation, action } = pendingAction
    try {
      if (action === 'approve') {
        await approveReservation.mutateAsync(reservation.id)
        showToast('Reservation approved.')
      } else if (action === 'cancel') {
        await cancelReservation.mutateAsync(reservation.id)
        showToast('Reservation cancelled.')
      }
      setPendingAction(null)
    } catch (error) {
      showToast(getErrorMessage(error), 'error')
    }
  }

  const handleRejectConfirm = async (reason: string) => {
    if (!pendingAction) return
    try {
      await rejectReservation.mutateAsync({ id: pendingAction.reservation.id, input: { rejectionReason: reason } })
      showToast('Reservation rejected.')
      setPendingAction(null)
    } catch (error) {
      showToast(getErrorMessage(error), 'error')
    }
  }

  return (
    <div className="mx-auto max-w-6xl">
      <div className="mb-6">
        <p className="text-sm text-ink-500">
          Review and manage energy slot reservations across all microgrid stations.
        </p>
      </div>

      <div className="mb-4 flex justify-end"><Button onClick={() => setEditor({})}>Create reservation</Button></div>
      {editor && <ReservationDialog reservation={editor.reservation} onClose={() => setEditor(null)} />}
      <ReservationSummaryCards summary={summary} isLoading={isSummaryLoading} />

      <div className="mt-6 rounded-2xl border border-ink-100 bg-white">
        <div className="flex gap-1 border-b border-ink-100 px-4 pt-3">
          {tabs.map((item) => (
            <button
              key={item.key}
              type="button"
              onClick={() => handleTabChange(item.key)}
              className={cn(
                'rounded-t-lg px-3 py-2 text-sm font-medium transition-colors',
                tab === item.key
                  ? 'border-b-2 border-brand-600 text-brand-700'
                  : 'text-ink-500 hover:text-ink-800',
              )}
            >
              {item.label}
            </button>
          ))}
        </div>

        <div className="border-b border-ink-100 p-4">
          <ReservationFilters filters={filters} onChange={setFilters} showStatusFilter={tab === 'history'} />
        </div>

        {isLoading ? (
          <LoadingState label="Loading reservations…" />
        ) : isError ? (
          <ErrorState message="Couldn't load reservations." onRetry={() => refetch()} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState title="No reservations found" description="Try adjusting your filters." />
        ) : (
          <>
            <ReservationTable reservations={data.items} onEdit={(reservation) => setEditor({ reservation })} onAction={handleAction} />
            <Pagination
              pageNumber={data.pageNumber}
              pageSize={data.pageSize}
              totalCount={data.totalCount}
              onPageChange={(page) => setFilters((current) => ({ ...current, pageNumber: page }))}
            />
          </>
        )}
      </div>

      <ConfirmDialog
        open={pendingAction !== null && pendingAction.action !== 'reject'}
        title={pendingAction?.action === 'approve' ? 'Approve reservation' : 'Cancel reservation'}
        description={
          pendingAction
            ? `Reservation ${pendingAction.reservation.id} will be ${
                pendingAction.action === 'approve' ? 'approved' : 'cancelled'
              }.`
            : undefined
        }
        confirmLabel={pendingAction?.action === 'approve' ? 'Approve' : 'Cancel reservation'}
        isDestructive={pendingAction?.action === 'cancel'}
        isLoading={isMutating}
        onConfirm={handleApproveOrCancelConfirm}
        onCancel={() => setPendingAction(null)}
      />

      <RejectReservationDialog
        open={pendingAction?.action === 'reject'}
        isLoading={isMutating}
        onConfirm={handleRejectConfirm}
        onCancel={() => setPendingAction(null)}
      />
    </div>
  )
}
