import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Button } from '@/components/common/Button'
import { Dialog } from '@/components/common/Dialog'
import { TextField } from '@/components/common/TextField'
import { digitsOnly, unsignedDecimal } from '@/lib/inputConstraints'
import { useToast } from '@/components/common/useToast'
import { FormError } from '@/features/microgrid/components/FormError'
import { useCreateMicrogridSlot, useUpdateMicrogridSlot } from '@/features/microgrid/hooks/useMicrogridSlotMutations'
import type { BatterySlotFormValues, MicrogridBatterySlot, MicrogridNode } from '@/features/microgrid/types'
import {
  defaultBatterySlotFormValues,
  toCreateSlotRequest,
  toScheduleWindows,
  toSlotFormValues,
  toUpdateSlotRequest,
  validateSlotRow,
} from '@/features/microgrid/validation'
import { getErrorMessage } from '@/lib/problemDetails'

interface BatterySlotDialogProps {
  open: boolean
  mode: 'create' | 'edit'
  node: MicrogridNode
  slot?: MicrogridBatterySlot
  onClose: () => void
}

export function BatterySlotDialog({ open, mode, node, slot, onClose }: BatterySlotDialogProps) {
  if (!open) return null

  return <BatterySlotDialogForm key={slot?.id ?? 'new'} mode={mode} node={node} slot={slot} onClose={onClose} />
}

function BatterySlotDialogForm({
  mode,
  node,
  slot,
  onClose,
}: {
  mode: 'create' | 'edit'
  node: MicrogridNode
  slot?: MicrogridBatterySlot
  onClose: () => void
}) {
  const { showToast } = useToast()
  const createSlot = useCreateMicrogridSlot(node.id)
  const updateSlot = useUpdateMicrogridSlot(node.id)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const isEdit = mode === 'edit'
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<BatterySlotFormValues>({
    defaultValues: slot ? toSlotFormValues(slot) : defaultBatterySlotFormValues,
  })

  const isSubmitting = createSlot.isPending || updateSlot.isPending

  const submit = handleSubmit(async (values) => {
    const rowError = validateSlotRow(values, toScheduleWindows(node.schedule))
    if (rowError) {
      setSubmitError(rowError)
      return
    }

    setSubmitError(null)
    try {
      if (isEdit && slot) {
        await updateSlot.mutateAsync({ id: slot.id, request: toUpdateSlotRequest(values) })
        showToast(`Battery slot ${slot.slotNumber} was updated.`)
      } else {
        const created = await createSlot.mutateAsync(toCreateSlotRequest(values))
        showToast(`Battery slot ${created.slotNumber} was added.`)
      }
      onClose()
    } catch (error) {
      setSubmitError(getErrorMessage(error, 'Could not save this battery slot.'))
    }
  })

  const formError = submitError

  return (
    <Dialog
      open
      onClose={onClose}
      size="lg"
      title={isEdit ? 'Edit battery slot' : 'Add battery slot'}
      description={
        isEdit
          ? 'Capacity and booking interval can change. Slot number stays the same.'
          : 'Storage capacity uses kWh. Booking times must sit inside the weekly hours.'
      }
    >
      <form onSubmit={submit} className="flex flex-col gap-4" noValidate>
        <FormError message={formError} />

        <p className="text-sm text-ink-500">Booking start and end use offset +00:00.</p>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <TextField
            label="Slot number"
            inputMode="numeric"
            sanitizeValue={(value) => digitsOnly(value, 10)}
            readOnly={isEdit}
            hint={isEdit ? 'Slot number cannot be changed after the slot is created.' : 'Must be unique on this node.'}
            error={errors.slotNumber?.message}
            {...register('slotNumber', {
              required: 'Battery storage slot number must be greater than zero.',
            })}
          />
          <TextField
            label="Storage capacity (kWh)"
            inputMode="decimal"
            sanitizeValue={unsignedDecimal}
            error={errors.batteryCapacityKwh?.message}
            {...register('batteryCapacityKwh', {
              required: 'Battery storage slot capacity in kWh must be greater than zero.',
            })}
          />
          <TextField
            label="Start date"
            type="date"
            error={errors.startDate?.message}
            {...register('startDate', { required: 'A booking slot must end after it starts.' })}
          />
          <TextField
            label="Start time"
            type="time"
            error={errors.startTime?.message}
            {...register('startTime', { required: 'A booking slot must end after it starts.' })}
          />
          <TextField
            label="End date"
            type="date"
            error={errors.endDate?.message}
            {...register('endDate', { required: 'A booking slot must end after it starts.' })}
          />
          <TextField
            label="End time"
            type="time"
            error={errors.endTime?.message}
            {...register('endTime', { required: 'A booking slot must end after it starts.' })}
          />
        </div>

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <Button type="button" variant="secondary" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" isLoading={isSubmitting}>
            {isEdit ? (isSubmitting ? 'Saving…' : 'Save changes') : isSubmitting ? 'Adding…' : 'Add slot'}
          </Button>
        </div>
      </form>
    </Dialog>
  )
}
