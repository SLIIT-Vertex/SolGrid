import { useState } from 'react'
import type { ChangeEvent, FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/common/Button'
import { Dialog } from '@/components/common/Dialog'
import { TextField } from '@/components/common/TextField'
import { useToast } from '@/components/common/useToast'
import { createProsumer, updateProsumer } from '@/features/prosumers/api'
import { prosumersKeys } from '@/features/prosumers/queryKeys'
import type { Prosumer } from '@/features/prosumers/types'
import {
  normalizeProsumerForm,
  PROSUMER_EMAIL_MAX_LENGTH,
  PROSUMER_NAME_MAX_LENGTH,
  PROSUMER_NIC_MAX_LENGTH,
  PROSUMER_PASSWORD_MAX_LENGTH,
  PROSUMER_PHONE_MAX_LENGTH,
  sanitizeEmailInput,
  sanitizeNameInput,
  sanitizeNicInput,
  sanitizePhoneInput,
  validateProsumerField,
  validateProsumerForm,
} from '@/features/prosumers/validation'
import type {
  ProsumerFormErrors,
  ProsumerFormField,
  ProsumerFormValues,
} from '@/features/prosumers/validation'
import { getErrorMessage } from '@/lib/problemDetails'

export function ProsumerDialog({ prosumer, onClose }: { prosumer?: Prosumer; onClose: () => void }) {
  const isCreate = !prosumer
  const [values, setValues] = useState<ProsumerFormValues>({
    nic: prosumer?.nic ?? '',
    firstName: prosumer?.firstName ?? '',
    lastName: prosumer?.lastName ?? '',
    email: prosumer?.email ?? '',
    phoneNumber: prosumer?.phoneNumber ?? '',
    password: '',
  })
  const [errors, setErrors] = useState<ProsumerFormErrors>({})
  const client = useQueryClient()
  const { showToast } = useToast()
  const save = useMutation({
    mutationFn: (request: ProsumerFormValues) => {
      const profile = {
        firstName: request.firstName,
        lastName: request.lastName,
        email: request.email,
        // The API models an omitted phone number as null, not an empty string.
        phoneNumber: request.phoneNumber || null,
      }
      return prosumer
        ? updateProsumer(prosumer.nic, profile)
        : createProsumer({ ...profile, nic: request.nic, password: request.password })
    },
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: prosumersKeys.all })
      showToast(prosumer ? 'Profile updated.' : 'Prosumer created. Activate the account when ready.')
      onClose()
    },
  })

  const setField = (field: ProsumerFormField, value: string) => {
    setValues((current) => ({ ...current, [field]: value }))
    // Clear the message while the user is correcting the field; re-check on blur.
    setErrors((current) => (current[field] ? { ...current, [field]: undefined } : current))
    if (save.isError) save.reset()
  }

  const validateField = (field: ProsumerFormField) => {
    setErrors((current) => ({ ...current, [field]: validateProsumerField(field, values, isCreate) }))
  }

  const fieldProps = (field: ProsumerFormField) => ({
    name: field,
    value: values[field],
    error: errors[field],
    onChange: (event: ChangeEvent<HTMLInputElement>) => setField(field, event.target.value),
    onBlur: () => validateField(field),
  })

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const normalized = normalizeProsumerForm(values)
    const nextErrors = validateProsumerForm(normalized, isCreate)
    setValues(normalized)
    setErrors(nextErrors)
    if (Object.keys(nextErrors).length > 0) return
    save.mutate(normalized)
  }

  const close = () => {
    if (!save.isPending) onClose()
  }

  return (
    <Dialog
      open
      onClose={close}
      title={prosumer ? 'Edit prosumer' : 'Create prosumer'}
      description={
        isCreate
          ? 'Prosumers normally register from the Android app. Creating here adds a Pending account that still needs activation.'
          : undefined
      }
      size="lg"
    >
      <form onSubmit={submit} className="grid gap-4" noValidate>
        <TextField
          {...fieldProps('nic')}
          label="NIC"
          readOnly={Boolean(prosumer)}
          required
          inputMode="text"
          autoCapitalize="characters"
          autoComplete="off"
          maxLength={PROSUMER_NIC_MAX_LENGTH}
          hint="12 digits, or 9 digits followed by V or X."
          sanitizeValue={sanitizeNicInput}
        />
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <TextField
            {...fieldProps('firstName')}
            label="First name"
            required
            autoComplete="given-name"
            maxLength={PROSUMER_NAME_MAX_LENGTH}
            sanitizeValue={sanitizeNameInput}
          />
          <TextField
            {...fieldProps('lastName')}
            label="Last name"
            required
            autoComplete="family-name"
            maxLength={PROSUMER_NAME_MAX_LENGTH}
            sanitizeValue={sanitizeNameInput}
          />
        </div>
        <TextField
          {...fieldProps('email')}
          label="Email"
          type="email"
          required
          autoComplete="email"
          maxLength={PROSUMER_EMAIL_MAX_LENGTH}
          sanitizeValue={sanitizeEmailInput}
        />
        <TextField
          {...fieldProps('phoneNumber')}
          label="Phone number"
          type="tel"
          autoComplete="tel"
          inputMode="numeric"
          maxLength={PROSUMER_PHONE_MAX_LENGTH}
          hint="Optional; enter 10 digits beginning with 0."
          sanitizeValue={sanitizePhoneInput}
        />
        {isCreate ? (
          <TextField
            {...fieldProps('password')}
            label="Password"
            type="password"
            required
            autoComplete="new-password"
            maxLength={PROSUMER_PASSWORD_MAX_LENGTH}
            hint="Use at least 8 characters."
          />
        ) : null}
        {save.isError ? (
          <p role="alert" className="text-sm text-red-600">
            {getErrorMessage(save.error)}
          </p>
        ) : null}
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" disabled={save.isPending} onClick={close}>
            Cancel
          </Button>
          <Button type="submit" isLoading={save.isPending}>
            Save
          </Button>
        </div>
      </form>
    </Dialog>
  )
}
