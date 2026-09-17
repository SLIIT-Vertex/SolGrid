import { useFieldArray, useFormContext } from 'react-hook-form'
import { Button } from '@/components/common/Button'
import { TextField } from '@/components/common/TextField'
import { FormError } from '@/features/microgrid/components/FormError'
import { ScheduleClockNote } from '@/features/microgrid/components/ScheduleClockNote'
import { WEEKDAYS } from '@/features/microgrid/types'
import type { ScheduleFormValues } from '@/features/microgrid/types'

export function ScheduleEditor() {
  const {
    control,
    register,
    watch,
    formState: { errors },
  } = useFormContext<ScheduleFormValues>()
  const { fields, append, remove } = useFieldArray({ control, name: 'schedule' })
  const schedule = watch('schedule')
  const scheduleError = errors.root?.schedule?.message

  return (
    <div className="flex flex-col gap-3">
      <ScheduleClockNote />
      <FormError message={scheduleError} />

      <div className="overflow-hidden rounded-xl border border-ink-100">
        {WEEKDAYS.map((weekday) => {
          const dayWindows = fields
            .map((field, index) => ({ field, index, day: schedule?.[index]?.day ?? field.day }))
            .filter(({ day }) => day === weekday.day)

          return (
            <div key={weekday.day} className="border-t border-ink-100 px-4 py-3 first:border-t-0">
              <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                <p className="pt-2 text-sm font-medium text-ink-800">{weekday.label}</p>
                <div className="flex min-w-0 flex-1 flex-col gap-2 sm:max-w-md sm:items-end">
                  {dayWindows.length === 0 ? (
                    <p className="text-sm text-ink-400">Closed</p>
                  ) : (
                    dayWindows.map(({ field, index }) => (
                      <div key={field.id} className="flex w-full flex-col gap-2 sm:flex-row sm:items-end">
                        <input type="hidden" {...register(`schedule.${index}.day`, { valueAsNumber: true })} />
                        <TextField
                          label="Opens"
                          type="time"
                          error={errors.schedule?.[index]?.opensAt?.message}
                          {...register(`schedule.${index}.opensAt`, {
                            required: 'An operating window must close after it opens.',
                          })}
                        />
                        <TextField
                          label="Closes"
                          type="time"
                          error={errors.schedule?.[index]?.closesAt?.message}
                          {...register(`schedule.${index}.closesAt`, {
                            required: 'An operating window must close after it opens.',
                          })}
                        />
                        <Button type="button" variant="ghost" className="sm:mb-0.5" onClick={() => remove(index)}>
                          Remove
                        </Button>
                      </div>
                    ))
                  )}
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    onClick={() => append({ day: weekday.day, opensAt: '08:00', closesAt: '17:00' })}
                  >
                    {dayWindows.length === 0 ? 'Add opening hours' : 'Add window'}
                  </Button>
                </div>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
