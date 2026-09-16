import { useState } from 'react'
import { Button } from '@/components/common/Button'
import { Dialog } from '@/components/common/Dialog'
import { TextField } from '@/components/common/TextField'

interface RejectReservationDialogProps {
  open: boolean
  isLoading?: boolean
  onConfirm: (reason: string) => void
  onCancel: () => void
}

export function RejectReservationDialog({ open, isLoading = false, onConfirm, onCancel }: RejectReservationDialogProps) {
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const handleClose = () => {
    if (isLoading) return
    setReason('')
    setError(null)
    onCancel()
  }

  const handleConfirm = () => {
    if (!reason.trim()) {
      setError('A rejection reason is required.')
      return
    }
    onConfirm(reason.trim())
  }

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      title="Reject reservation"
      description="Provide a reason the prosumer will see."
      footer={
        <>
          <Button variant="secondary" type="button" onClick={handleClose} disabled={isLoading}>
            Cancel
          </Button>
          <Button type="button" variant="danger" onClick={handleConfirm} isLoading={isLoading}>
            Reject
          </Button>
        </>
      }
    >
      <TextField
        label="Rejection reason"
        value={reason}
        onChange={(event) => {
          setReason(event.target.value)
          setError(null)
        }}
        error={error ?? undefined}
        placeholder="e.g. Station under maintenance at this time"
      />
    </Dialog>
  )
}
