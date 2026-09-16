import { useState } from 'react'
import { FormProvider, useForm } from 'react-hook-form'
import { Button } from '@/components/common/Button'
import { useToast } from '@/components/common/useToast'
import { FormError } from '@/features/microgrid/components/FormError'
import { ScheduleEditor } from '@/features/microgrid/components/ScheduleEditor'
import { ScheduleView } from '@/features/microgrid/components/ScheduleView'
import { useUpdateMicrogridSchedule } from '@/features/microgrid/hooks/useUpdateMicrogridSchedule'
import { SCHEDULE_REPLACE_NOTE } from '@/features/microgrid/types'
import type { MicrogridNode, ScheduleFormValues } from '@/features/microgrid/types'
import { toScheduleFormValues, toUpdateScheduleRequest, validateScheduleWindows } from '@/features/microgrid/validation'
import { getErrorMessage } from '@/lib/problemDetails'

export function ScheduleTab({ node, canEdit }: { node: MicrogridNode; canEdit: boolean }) {
  const [isEditing, setIsEditing] = useState(false)

  return (
    <div>
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-sm font-semibold text-ink-900">Operating schedule</h3>
          <p className="mt-1 text-sm text-ink-500">
            {isEditing
              ? 'This replaces the full weekly configuration. Closed days have no windows.'
              : 'Weekly opening hours for this node.'}
          </p>
        </div>
        {canEdit && !isEditing ? (
          <Button type="button" variant="secondary" className="w-full sm:w-auto" onClick={() => setIsEditing(true)}>
            Edit schedule
          </Button>
        ) : null}
      </div>

      {isEditing && canEdit ? (
        <ScheduleEditForm node={node} onCancel={() => setIsEditing(false)} onSaved={() => setIsEditing(false)} />
      ) : (
        <ScheduleView schedule={node.schedule} />
      )}
    </div>
  )
}

function ScheduleEditForm({
  node,
  onCancel,
  onSaved,
}: {
  node: MicrogridNode
  onCancel: () => void
  onSaved: () => void
}) {
  const { showToast } = useToast()
  const updateSchedule = useUpdateMicrogridSchedule(node.id)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const methods = useForm<ScheduleFormValues>({
    defaultValues: toScheduleFormValues(node.schedule),
  })
  const { handleSubmit, setError, clearErrors } = methods

  const submit = handleSubmit(async (values) => {
    clearErrors()
    const scheduleError = validateScheduleWindows(values.schedule)
    if (scheduleError) {
      setError('root.schedule', { type: 'validate', message: scheduleError })
      return
    }

    setSubmitError(null)
    try {
      await updateSchedule.mutateAsync(toUpdateScheduleRequest(values))
      showToast(`Operating hours for ${node.name} were updated.`)
      onSaved()
    } catch (error) {
      setSubmitError(getErrorMessage(error, 'Could not update this operating schedule.'))
    }
  })

  return (
    <FormProvider {...methods}>
      <form onSubmit={submit} className="flex flex-col gap-4" noValidate>
        <FormError message={submitError} />
        <p className="text-sm text-ink-500">{SCHEDULE_REPLACE_NOTE}</p>
        <ScheduleEditor />
        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <Button type="button" variant="secondary" onClick={onCancel} disabled={updateSchedule.isPending}>
            Cancel
          </Button>
          <Button type="submit" isLoading={updateSchedule.isPending}>
            {updateSchedule.isPending ? 'Saving…' : 'Save schedule'}
          </Button>
        </div>
      </form>
    </FormProvider>
  )
}
