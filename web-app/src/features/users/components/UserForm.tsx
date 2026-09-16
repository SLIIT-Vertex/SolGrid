import { useForm } from 'react-hook-form'
import { Button } from '@/components/common/Button'
import { TextField } from '@/components/common/TextField'
import { SelectField } from '@/components/common/SelectField'
import type { UserRole } from '@/auth/types'

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
  onSubmit: (values: UserFormValues) => void
  onCancel: () => void
}

export function UserForm({
  mode,
  defaultValues,
  isSubmitting = false,
  submitError,
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
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4" noValidate>
      {submitError ? (
        <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{submitError}</div>
      ) : null}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <TextField
          label="First name"
          error={errors.firstName?.message}
          {...register('firstName', { required: 'First name is required' })}
        />
        <TextField
          label="Last name"
          error={errors.lastName?.message}
          {...register('lastName', { required: 'Last name is required' })}
        />
      </div>

      <TextField
        label="Email"
        type="email"
        error={errors.email?.message}
        {...register('email', {
          required: 'Email is required',
          pattern: { value: /^\S+@\S+\.\S+$/, message: 'Enter a valid email address' },
        })}
      />

      <SelectField label="Role" error={errors.role?.message} {...register('role', { required: true })}>
        <option value="Backoffice">Backoffice</option>
        <option value="GridOperator">Grid Operator</option>
      </SelectField>

      {mode === 'create' ? (
        <TextField
          label="Temporary password"
          type="password"
          hint="The user should change this after first login."
          error={errors.password?.message}
          {...register('password', {
            required: 'Password is required',
            minLength: { value: 8, message: 'Password must be at least 8 characters' },
          })}
        />
      ) : null}

      <div className="mt-2 flex justify-end gap-2">
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" isLoading={isSubmitting}>
          {mode === 'create' ? 'Create user' : 'Save changes'}
        </Button>
      </div>
    </form>
  )
}
