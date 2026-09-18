import { useState } from 'react'
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

interface StepDefinition {
  title: string
  description: string
  fields?: (keyof MicrogridNodeFormValues)[]
}

// Edit mode only ever touches identity/location/capacity — schedule and battery slots are managed
// separately from the node's own details page once it exists, so editing gets a shorter wizard.
const CREATE_STEPS: StepDefinition[] = [
  {
    title: 'Node identity & capacity',
    description: "Code, name, address (with location on the map), and the station's generation capacity.",
    fields: ['code', 'name', 'addressLine', 'latitude', 'longitude', 'capacityKw'],
  },
  {
    title: 'Operating schedule',
    description:
      'At least one weekly opening window is required. Same-day windows may touch but must not overlap. Overnight hours are not supported.',
  },
  {
    title: 'Initial battery slots',
    description: 'Optional. Storage capacity uses kWh. Slot times must sit inside the weekly hours.',
  },
]

const EDIT_STEPS: StepDefinition[] = CREATE_STEPS.slice(0, 1)

export function NodeForm(props: NodeFormProps) {
  const { isSubmitting = false, submitError, onCancel } = props
  const isEdit = props.mode === 'edit'
  const steps = isEdit ? EDIT_STEPS : CREATE_STEPS
  const [stepIndex, setStepIndex] = useState(0)
  // Tracks the furthest step reached so far, so the step-number dots can be clicked to jump back
  // to any already-visited step without re-validating, while still blocking a jump ahead to a step
  // that hasn't been reached yet (which would skip its own validation).
  const [maxUnlockedStep, setMaxUnlockedStep] = useState(0)
  const [stepError, setStepError] = useState<string | null>(null)

  const methods = useForm<MicrogridNodeFormValues>({
    defaultValues: props.defaultValues ?? defaultCreateNodeValues,
  })
  const {
    register,
    handleSubmit,
    trigger,
    getValues,
    setError,
    clearErrors,
    formState: { errors },
  } = methods

  const isLastStep = stepIndex === steps.length - 1

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

  async function goToNextStep() {
    setStepError(null)
    const currentStep = steps[stepIndex]

    if (currentStep.fields) {
      const isStepValid = await trigger(currentStep.fields)
      if (!isStepValid) return
    } else if (currentStep.title === 'Operating schedule') {
      const scheduleError = validateScheduleWindows(getValues('schedule'))
      if (scheduleError) {
        setStepError(scheduleError)
        return
      }
    }

    const nextIndex = Math.min(stepIndex + 1, steps.length - 1)
    setStepIndex(nextIndex)
    setMaxUnlockedStep((max) => Math.max(max, nextIndex))
  }

  function goToPreviousStep() {
    setStepError(null)
    setStepIndex((index) => Math.max(index - 1, 0))
  }

  function goToStep(index: number) {
    if (index > maxUnlockedStep) return
    setStepError(null)
    setStepIndex(index)
  }

  function handleFormSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!isLastStep) {
      void goToNextStep()
      return
    }
    void submit(event)
  }

  const currentStep = steps[stepIndex]

  return (
    <FormProvider {...methods}>
      <form onSubmit={handleFormSubmit} className="flex min-h-0 flex-1 flex-col gap-4" noValidate>
        <FormError message={submitError} />

        {/* Progress indicator: a clickable number per step. Completed/current steps are filled and
            can be jumped back to directly; steps not yet reached stay locked to force validation
            in order. */}
        <ol className="flex items-center gap-2 px-1">
          {steps.map((step, index) => {
            const isUnlocked = index <= maxUnlockedStep
            const isLastStepInList = index === steps.length - 1
            return (
              <li key={step.title} className={['flex items-center gap-2', isLastStepInList ? '' : 'flex-1'].join(' ')}>
                <button
                  type="button"
                  onClick={() => goToStep(index)}
                  disabled={!isUnlocked}
                  aria-current={index === stepIndex ? 'step' : undefined}
                  aria-label={`Step ${index + 1}: ${step.title}`}
                  className={[
                    'flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-xs font-semibold transition-colors',
                    index < stepIndex
                      ? 'bg-brand-600 text-white hover:bg-brand-700'
                      : index === stepIndex
                        ? 'border-2 border-brand-600 text-brand-600'
                        : isUnlocked
                          ? 'border border-ink-200 text-ink-400 hover:border-brand-300'
                          : 'cursor-not-allowed border border-ink-100 text-ink-300',
                  ].join(' ')}
                >
                  {index + 1}
                </button>
                {index < steps.length - 1 ? (
                  <span className={['h-0.5 flex-1', index < stepIndex ? 'bg-brand-600' : 'bg-ink-100'].join(' ')} />
                ) : null}
              </li>
            )
          })}
        </ol>
        <p className="text-xs font-medium uppercase tracking-wide text-ink-400">
          Step {stepIndex + 1} of {steps.length}
        </p>

        <div className="min-h-0 flex-1 overflow-y-auto pr-1">
          <MicrogridSection title={currentStep.title} description={currentStep.description}>
            {currentStep.title === 'Node identity & capacity' ? (
              <div className="flex flex-col gap-4">
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

                <AddressAutocompleteField />

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
              </div>
            ) : null}

            {currentStep.title === 'Operating schedule' ? (
              <>
                <ScheduleEditor />
                <FormError message={stepError ?? errors.root?.schedule?.message} />
              </>
            ) : null}

            {currentStep.title === 'Initial battery slots' ? <BatterySlotForm /> : null}
          </MicrogridSection>
        </div>

        <div className="flex flex-col-reverse gap-2 border-t border-ink-100 pt-4 sm:flex-row sm:justify-between">
          <Button type="button" variant="secondary" onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </Button>
          <div className="flex flex-col-reverse gap-2 sm:flex-row">
            {stepIndex > 0 ? (
              <Button type="button" variant="secondary" onClick={goToPreviousStep} disabled={isSubmitting}>
                Back
              </Button>
            ) : null}
            {isLastStep ? (
              <Button type="submit" isLoading={isSubmitting}>
                {isEdit ? (isSubmitting ? 'Saving…' : 'Save changes') : isSubmitting ? 'Creating…' : 'Create Node'}
              </Button>
            ) : (
              <Button type="submit">Next</Button>
            )}
          </div>
        </div>
      </form>
    </FormProvider>
  )
}
