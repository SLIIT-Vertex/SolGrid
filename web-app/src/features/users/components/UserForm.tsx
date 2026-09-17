import { useForm } from 'react-hook-form'
import { Button } from '@/components/common/Button'
import { TextField } from '@/components/common/TextField'
import { SelectField } from '@/components/common/SelectField'
import type { UserRole } from '@/auth/types'
import {
  normalizeUserFormValues,
  USER_EMAIL_MAX_LENGTH,
  USER_NAME_MAX_LENGTH,
  USER_PASSWORD_MAX_LENGTH,
  validateEmail,
  validateName,
  validatePassword,
} from '@/features/users/validation'

export interface UserFormValues {
  firstName: string
  lastName: string
  email: string
  role: UserRole
  password?: string
}

interface UserFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<UserFormValues>
  isSubmitting?: boolean
  submitError?: string | null
  lockRole?: boolean
  onSubmit: (values: UserFormValues) => void
  onCancel: () => void
}

export function UserForm({
  mode,
  defaultValues,
  isSubmitting = false,
  submitError,
  lockRole = false,
  onSubmit,
  onCancel,
}: UserFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<UserFormValues>({
    defaultValues: {
      firstName: defaultValues?.firstName ?? '',
      lastName: defaultValues?.lastName ?? '',
      email: defaultValues?.email ?? '',
      role: defaultValues?.role ?? 'GridOperator',
      password: '',
    },
  })

  return (
    <form
      onSubmit={handleSubmit((values) => onSubmit(normalizeUserFormValues(values)))}
      className="flex flex-col gap-4"
      noValidate
    >
      {submitError ? (
        <div role="alert" aria-live="polite" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
          {submitError}
        </div>
      ) : null}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <TextField
          label="First name"
          autoComplete="given-name"
          maxLength={USER_NAME_MAX_LENGTH}
          error={errors.firstName?.message}
          {...register('firstName', { validate: (value) => validateName(value, 'First name') })}
        />
        <TextField
          label="Last name"
          autoComplete="family-name"
          maxLength={USER_NAME_MAX_LENGTH}
          error={errors.lastName?.message}
          {...register('lastName', { validate: (value) => validateName(value, 'Last name') })}
        />
      </div>

      <TextField
        label="Email"
        type="email"
        autoComplete="email"
        maxLength={USER_EMAIL_MAX_LENGTH}
        error={errors.email?.message}
        {...register('email', { validate: validateEmail })}
      />

      {lockRole ? (
        <>
          <TextField
            label="Role"
            value={defaultValues?.role === 'Backoffice' ? 'Backoffice' : 'Grid Operator'}
            hint="You cannot change your own role."
            readOnly
          />
          <input type="hidden" {...register('role')} />
        </>
      ) : (
        <SelectField label="Role" error={errors.role?.message} {...register('role', { required: true })}>
          <option value="Backoffice">Backoffice</option>
          <option value="GridOperator">Grid Operator</option>
        </SelectField>
      )}

      {mode === 'create' ? (
        <TextField
          label="Temporary password"
          type="password"
          autoComplete="new-password"
          maxLength={USER_PASSWORD_MAX_LENGTH}
          hint="Use at least 8 characters and share it securely."
          error={errors.password?.message}
          {...register('password', { validate: validatePassword })}
        />
      ) : null}

      <div className="mt-2 flex justify-end gap-2">
        <Button type="button" variant="secondary" onClick={onCancel} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button type="submit" isLoading={isSubmitting}>
          {mode === 'create' ? 'Create user' : 'Save changes'}
        </Button>
      </div>
    </form>
  )
}
