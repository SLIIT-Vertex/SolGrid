import { useFieldArray, useFormContext, useWatch } from 'react-hook-form'
import { Button } from '@/components/common/Button'
import { TextField } from '@/components/common/TextField'
import { digitsOnly, unsignedDecimal } from '@/lib/inputConstraints'
import { FormError } from '@/features/microgrid/components/FormError'
import { SlotDateCalendar } from '@/features/microgrid/components/SlotDateCalendar'
import { getDayOperatingRange, validateSlotRow } from '@/features/microgrid/validation'
import type { MicrogridNodeFormValues } from '@/features/microgrid/types'

export function BatterySlotForm() {
  const {
    control,
    register,
    setValue,
    formState: { errors },
  } = useFormContext<MicrogridNodeFormValues>()
  const { fields, append, remove } = useFieldArray({ control, name: 'slots' })
  const slotsError = errors.root?.slots?.message
  const schedule = useWatch({ control, name: 'schedule' })
  const slotValues = useWatch({ control, name: 'slots' })

  return (
    <div className="flex flex-col gap-3">
      <FormError message={slotsError} />

      {fields.length === 0 ? (
        <p className="text-sm text-ink-500">No initial battery slots. You can add them later from the node details.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {fields.map((field, index) => {
            const slot = slotValues?.[index]
            const hasCompleteRange = Boolean(slot?.startDate && slot.startTime && slot.endDate && slot.endTime)
            const scheduleError = hasCompleteRange ? validateSlotRow(slot, schedule ?? []) : undefined
            const startDayRange = slot?.startDate
              ? getDayOperatingRange(schedule ?? [], new Date(`${slot.startDate}T00:00:00`).getDay())
              : undefined
            const endDayRange = slot?.endDate
              ? getDayOperatingRange(schedule ?? [], new Date(`${slot.endDate}T00:00:00`).getDay())
              : undefined

            return (
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
                    type="number"
                    step="1"
                    min="1"
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
                    type="number"
                    step="any"
                    min="0"
                    inputMode="decimal"
                    sanitizeValue={unsignedDecimal}
                    error={errors.slots?.[index]?.batteryCapacityKwh?.message}
                    {...register(`slots.${index}.batteryCapacityKwh`, {
                      required: 'Battery storage slot capacity in kWh must be greater than zero.',
                    })}
                  />
                  <input type="hidden" {...register(`slots.${index}.startDate`, { required: 'A booking slot must end after it starts.' })} />
                  <input type="hidden" {...register(`slots.${index}.endDate`, { required: 'A booking slot must end after it starts.' })} />
                  <SlotDateCalendar
                    label="Start date"
                    value={slot?.startDate ?? ''}
                    schedule={schedule ?? []}
                    error={errors.slots?.[index]?.startDate?.message ?? scheduleError}
                    onSelect={(date) => setValue(`slots.${index}.startDate`, date, { shouldValidate: true, shouldDirty: true })}
                  />
                  <SlotDateCalendar
                    label="End date"
                    value={slot?.endDate ?? ''}
                    schedule={schedule ?? []}
                    minDate={slot?.startDate}
                    error={errors.slots?.[index]?.endDate?.message}
                    onSelect={(date) => setValue(`slots.${index}.endDate`, date, { shouldValidate: true, shouldDirty: true })}
                  />
                  <TextField
                    label="Start time"
                    type="time"
                    min={startDayRange?.min}
                    max={startDayRange?.max}
                    hint={
                      startDayRange
                        ? `Station is open ${startDayRange.min}–${startDayRange.max} on this day.`
                        : slot?.startDate
                          ? 'Station is not open on this day of the week.'
                          : undefined
                    }
                    error={errors.slots?.[index]?.startTime?.message ?? scheduleError}
                    {...register(`slots.${index}.startTime`, {
                      required: 'A booking slot must end after it starts.',
                    })}
                  />
                  <TextField
                    label="End time"
                    type="time"
                    min={endDayRange?.min}
                    max={endDayRange?.max}
                    hint={
                      endDayRange
                        ? `Station is open ${endDayRange.min}–${endDayRange.max} on this day.`
                        : slot?.endDate
                          ? 'Station is not open on this day of the week.'
                          : undefined
                    }
                    error={errors.slots?.[index]?.endTime?.message}
                    {...register(`slots.${index}.endTime`, {
                      required: 'A booking slot must end after it starts.',
                    })}
                  />
                </div>
              </div>
            )
          })}
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
