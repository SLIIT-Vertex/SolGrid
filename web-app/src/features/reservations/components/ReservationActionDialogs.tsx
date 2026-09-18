import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import type { PendingReservationAction } from '../types'
import { RejectReservationDialog } from './RejectReservationDialog'

interface ReservationActionDialogsProps {
  pendingAction: PendingReservationAction | null
  isLoading: boolean
  onConfirm: () => void
  onReject: (reason: string) => void
  onCancel: () => void
}

export function ReservationActionDialogs({
  pendingAction,
  isLoading,
  onConfirm,
  onReject,
  onCancel,
}: ReservationActionDialogsProps) {
  return (
    <>
      <ConfirmDialog
        open={pendingAction !== null && pendingAction.action !== 'reject'}
        title={
          pendingAction?.action === 'approve'
            ? 'Approve reservation'
            : 'Cancel reservation'
        }
        description={
          pendingAction
            ? `${pendingAction.reservation.referenceCode} will be ${
                pendingAction.action === 'approve' ? 'approved' : 'cancelled'
              }.`
            : undefined
        }
        confirmLabel={
          pendingAction?.action === 'approve' ? 'Approve' : 'Cancel reservation'
        }
        confirmClassName={
          pendingAction?.action === 'approve'
            ? 'reservation-primary'
            : undefined
        }
        isDestructive={pendingAction?.action === 'cancel'}
        isLoading={isLoading}
        onConfirm={onConfirm}
        onCancel={onCancel}
      />

      <RejectReservationDialog
        open={pendingAction?.action === 'reject'}
        isLoading={isLoading}
        onConfirm={onReject}
        onCancel={onCancel}
      />
    </>
  )
}
