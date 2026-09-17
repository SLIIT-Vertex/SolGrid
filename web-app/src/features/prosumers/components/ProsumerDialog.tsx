import { useState } from 'react'
import type { FormEvent } from 'react'
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
  sanitizeNicInput,
  sanitizePhoneInput,
  validateProsumerForm,
} from '@/features/prosumers/validation'
import type { ProsumerFormErrors, ProsumerFormValues } from '@/features/prosumers/validation'
import { getErrorMessage } from '@/lib/problemDetails'

export function ProsumerDialog({ prosumer, onClose }: { prosumer?: Prosumer; onClose: () => void }) {
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
    mutationFn: (request: ProsumerFormValues) =>
      prosumer ? updateProsumer(prosumer.nic, request) : createProsumer(request),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: prosumersKeys.all })
      showToast(prosumer ? 'Profile updated.' : 'Prosumer created. Activate the account when ready.')
      onClose()
    },
  })

  const setField = (field: keyof ProsumerFormValues, value: string) => {
    setValues((current) => ({ ...current, [field]: value }))
    setErrors((current) => ({ ...current, [field]: undefined }))
    if (save.isError) save.reset()
  }

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const nextErrors = validateProsumerForm(values, !prosumer)
    setErrors(nextErrors)
    if (Object.keys(nextErrors).length > 0) return
    save.mutate(normalizeProsumerForm(values))
  }

  const close = () => {
    if (!save.isPending) onClose()
  }

  return (
    <Dialog open onClose={close} title={prosumer ? 'Edit prosumer' : 'Create prosumer'} size="lg">
      <form onSubmit={submit} className="grid gap-4" noValidate>
        <TextField
          name="nic"
          label="NIC"
          value={values.nic}
          readOnly={Boolean(prosumer)}
          required
          inputMode="text"
          autoCapitalize="characters"
          maxLength={PROSUMER_NIC_MAX_LENGTH}
          hint="12 digits, or 9 digits followed by V or X."
          error={errors.nic}
          sanitizeValue={sanitizeNicInput}
          onChange={(event) => setField('nic', event.target.value)}
        />
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <TextField
            name="firstName"
            label="First name"
            value={values.firstName}
            required
            autoComplete="given-name"
            maxLength={PROSUMER_NAME_MAX_LENGTH}
            error={errors.firstName}
            onChange={(event) => setField('firstName', event.target.value)}
          />
          <TextField
            name="lastName"
            label="Last name"
            value={values.lastName}
            required
            autoComplete="family-name"
            maxLength={PROSUMER_NAME_MAX_LENGTH}
            error={errors.lastName}
            onChange={(event) => setField('lastName', event.target.value)}
          />
        </div>
        <TextField
          name="email"
          label="Email"
          type="email"
          value={values.email}
          required
          autoComplete="email"
          maxLength={PROSUMER_EMAIL_MAX_LENGTH}
          error={errors.email}
          onChange={(event) => setField('email', event.target.value)}
        />
        <TextField
          name="phoneNumber"
          label="Phone number"
          type="tel"
          value={values.phoneNumber}
          autoComplete="tel"
          inputMode="tel"
          maxLength={PROSUMER_PHONE_MAX_LENGTH}
          hint="Optional; enter 10 digits beginning with 0."
          error={errors.phoneNumber}
          sanitizeValue={sanitizePhoneInput}
          onChange={(event) => setField('phoneNumber', event.target.value)}
        />
        {!prosumer ? (
          <TextField
            name="password"
            label="Password"
            type="password"
            value={values.password}
            required
            autoComplete="new-password"
            maxLength={PROSUMER_PASSWORD_MAX_LENGTH}
            hint="Use at least 8 characters."
            error={errors.password}
            onChange={(event) => setField('password', event.target.value)}
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
