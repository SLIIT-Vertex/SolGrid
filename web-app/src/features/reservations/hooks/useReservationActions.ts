import { useState } from 'react'
import { useToast } from '@/components/common/useToast'
import { getErrorMessage } from '@/lib/problemDetails'
import type {
  PendingReservationAction,
  Reservation,
  ReservationAction,
} from '../types'
import {
  useApproveReservation,
  useCancelReservation,
  useRejectReservation,
} from './useReviewReservation'

export function useReservationActions() {
  const [pendingAction, setPendingAction] =
    useState<PendingReservationAction | null>(null)
  const approveReservation = useApproveReservation()
  const rejectReservation = useRejectReservation()
  const cancelReservation = useCancelReservation()
  const { showToast } = useToast()

  const isMutating =
    approveReservation.isPending ||
    rejectReservation.isPending ||
    cancelReservation.isPending

  const requestAction = (
    reservation: Reservation,
    action: ReservationAction,
  ) => {
    setPendingAction({ reservation, action })
  }
  const dismissAction = () => setPendingAction(null)

  const confirmApprovalOrCancellation = async () => {
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

  const confirmRejection = async (reason: string) => {
    if (!pendingAction) return
    try {
      await rejectReservation.mutateAsync({
        id: pendingAction.reservation.id,
        input: { rejectionReason: reason },
      })
      showToast('Reservation rejected.')
      setPendingAction(null)
    } catch (error) {
      showToast(getErrorMessage(error), 'error')
    }
  }

  return {
    pendingAction,
    isMutating,
    requestAction,
    dismissAction,
    confirmApprovalOrCancellation,
    confirmRejection,
  }
}
