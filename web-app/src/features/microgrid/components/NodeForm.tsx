import { FormProvider, useForm } from 'react-hook-form'
import { Button } from '@/components/common/Button'
import { TextField } from '@/components/common/TextField'
import { AddressAutocompleteField } from '@/features/microgrid/components/AddressAutocompleteField'
import { BatterySlotForm } from '@/features/microgrid/components/BatterySlotForm'
import { FormError } from '@/features/microgrid/components/FormError'
import { MicrogridSection } from '@/features/microgrid/components/MicrogridSection'
import { ScheduleEditor } from '@/features/microgrid/components/ScheduleEditor'
import { MAX_CODE_LENGTH, MAX_NAME_LENGTH, defaultCreateNodeValues } from '@/features/microgrid/types'
import { unsignedDecimal } from '@/lib/inputConstraints'
import type {
  CreateSolarStationRequest,
  MicrogridNodeFormValues,
  UpdateSolarStationRequest,
} from '@/features/microgrid/types'
import {
  toCreateStationRequest,
  toUpdateStationRequest,
  validateCode,
  validateInitialSlots,
  validateName,
  validatePositiveCapacity,
  validateScheduleWindows,
} from '@/features/microgrid/validation'

interface NodeFormBase {
  defaultValues?: MicrogridNodeFormValues
  isSubmitting?: boolean
  submitError?: string | null
  onCancel: () => void
}

type NodeFormProps = NodeFormBase &
  (
    | { mode?: 'create'; onSubmit: (request: CreateSolarStationRequest) => void }
    | { mode: 'edit'; onSubmit: (request: UpdateSolarStationRequest) => void }
  )

export function NodeForm(props: NodeFormProps) {
  const { isSubmitting = false, submitError, onCancel } = props
  const isEdit = props.mode === 'edit'
  const methods = useForm<MicrogridNodeFormValues>({
    defaultValues: props.defaultValues ?? defaultCreateNodeValues,
  })
  const {
    register,
    handleSubmit,
    setError,
    clearErrors,
    formState: { errors },
  } = methods

  const submit = handleSubmit((values) => {
    if (props.mode === 'edit') {
      props.onSubmit(toUpdateStationRequest(values))
      return
    }

    clearErrors()

    const scheduleError = validateScheduleWindows(values.schedule)
    if (scheduleError) {
      setError('root.schedule', { type: 'validate', message: scheduleError })
      return
    }

    const slotsError = validateInitialSlots(values.slots, values.schedule)
    if (slotsError) {
      setError('root.slots', { type: 'validate', message: slotsError })
      return
    }

    props.onSubmit(toCreateStationRequest(values))
  })

  return (
    <FormProvider {...methods}>
      <form onSubmit={submit} className="flex flex-col gap-4" noValidate>
        <FormError message={submitError} />

        <MicrogridSection
          title={isEdit ? 'Node identity' : '1. Node identity'}
          description="Code and name for this generation node."
        >
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <TextField
              label="Code"
              maxLength={MAX_CODE_LENGTH}
              autoComplete="off"
              error={errors.code?.message}
              {...register('code', { validate: (value) => validateCode(value) ?? true })}
            />
            <TextField
              label="Name"
              maxLength={MAX_NAME_LENGTH}
              error={errors.name?.message}
              {...register('name', { validate: (value) => validateName(value) ?? true })}
            />
          </div>
        </MicrogridSection>

        <MicrogridSection
          title={isEdit ? 'Location' : '2. Location'}
          description="Search for the station's address — latitude and longitude are filled in automatically."
        >
          <AddressAutocompleteField />
        </MicrogridSection>

        <MicrogridSection
          title={isEdit ? 'Generation capacity' : '3. Generation capacity'}
          description="Installed generation capacity in kW."
        >
          <TextField
            label="Generation capacity (kW)"
            type="number"
            step="any"
            min="0"
            inputMode="decimal"
            sanitizeValue={unsignedDecimal}
            error={errors.capacityKw?.message}
            {...register('capacityKw', {
              validate: (value) =>
                validatePositiveCapacity(value, 'Station capacity in kW must be greater than zero.') ?? true,
            })}
          />
        </MicrogridSection>

        {isEdit ? null : (
          <>
            <MicrogridSection
              title="4. Operating schedule"
              description="At least one weekly opening window is required. Same-day windows may touch but must not overlap. Overnight hours are not supported."
            >
              <ScheduleEditor />
            </MicrogridSection>

            <MicrogridSection
              title="5. Initial battery slots"
              description="Optional. Storage capacity uses kWh. Slot times must sit inside the weekly hours."
            >
              <BatterySlotForm />
            </MicrogridSection>
          </>
        )}

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <Button type="button" variant="secondary" onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" isLoading={isSubmitting}>
            {isEdit
              ? isSubmitting
                ? 'Saving…'
                : 'Save changes'
              : isSubmitting
                ? 'Creating…'
                : 'Create Node'}
          </Button>
        </div>
      </form>
    </FormProvider>
  )
}
