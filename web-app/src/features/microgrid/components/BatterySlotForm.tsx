import { useFieldArray, useFormContext } from 'react-hook-form'
import { Button } from '@/components/common/Button'
import { TextField } from '@/components/common/TextField'
import { digitsOnly, unsignedDecimal } from '@/lib/inputConstraints'
import { FormError } from '@/features/microgrid/components/FormError'
import type { MicrogridNodeFormValues } from '@/features/microgrid/types'

export function BatterySlotForm() {
  const {
    control,
    register,
    formState: { errors },
  } = useFormContext<MicrogridNodeFormValues>()
  const { fields, append, remove } = useFieldArray({ control, name: 'slots' })
  const slotsError = errors.root?.slots?.message

  return (
    <div className="flex flex-col gap-3">
      <FormError message={slotsError} />

      {fields.length === 0 ? (
        <p className="text-sm text-ink-500">No initial battery slots. You can add them later from the node details.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {fields.map((field, index) => (
            <div key={field.id} className="rounded-xl border border-ink-100 p-4">
              <div className="mb-3 flex items-center justify-between gap-3">
                <p className="text-sm font-medium text-ink-800">Battery slot {index + 1}</p>
                <Button type="button" variant="ghost" size="sm" onClick={() => remove(index)}>
                  Remove
                </Button>
              </div>
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <TextField
                  label="Slot number"
                  inputMode="numeric"
                  sanitizeValue={(value) => digitsOnly(value, 10)}
                  hint="Must be unique on this node."
                  error={errors.slots?.[index]?.slotNumber?.message}
                  {...register(`slots.${index}.slotNumber`, {
                    required: 'Battery storage slot number must be greater than zero.',
                  })}
                />
                <TextField
                  label="Storage capacity (kWh)"
                  inputMode="decimal"
                  sanitizeValue={unsignedDecimal}
                  error={errors.slots?.[index]?.batteryCapacityKwh?.message}
                  {...register(`slots.${index}.batteryCapacityKwh`, {
                    required: 'Battery storage slot capacity in kWh must be greater than zero.',
                  })}
                />
                <TextField
                  label="Start date"
                  type="date"
                  error={errors.slots?.[index]?.startDate?.message}
                  {...register(`slots.${index}.startDate`, {
                    required: 'A booking slot must end after it starts.',
                  })}
                />
                <TextField
                  label="Start time"
                  type="time"
                  error={errors.slots?.[index]?.startTime?.message}
                  {...register(`slots.${index}.startTime`, {
                    required: 'A booking slot must end after it starts.',
                  })}
                />
                <TextField
                  label="End date"
                  type="date"
                  error={errors.slots?.[index]?.endDate?.message}
                  {...register(`slots.${index}.endDate`, {
                    required: 'A booking slot must end after it starts.',
                  })}
                />
                <TextField
                  label="End time"
                  type="time"
                  error={errors.slots?.[index]?.endTime?.message}
                  {...register(`slots.${index}.endTime`, {
                    required: 'A booking slot must end after it starts.',
                  })}
                />
              </div>
            </div>
          ))}
        </div>
      )}

      <Button
        type="button"
        variant="secondary"
        onClick={() =>
          append({
            slotNumber: String(fields.length + 1),
            batteryCapacityKwh: '',
            startDate: '',
            startTime: '08:00',
            endDate: '',
            endTime: '10:00',
          })
        }
      >
        Add battery slot
      </Button>
    </div>
  )
}
