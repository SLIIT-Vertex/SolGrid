import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { Button } from '@/components/common/Button'
import { Dialog } from '@/components/common/Dialog'
import { TextField } from '@/components/common/TextField'
import { useToast } from '@/components/common/useToast'
import { useResetUserPassword } from '@/features/users/hooks/useResetUserPassword'
import { USER_PASSWORD_MAX_LENGTH, validatePassword } from '@/features/users/validation'
import type { User } from '@/features/users/types'
import { getErrorMessage } from '@/lib/problemDetails'

interface ResetPasswordFormValues {
  newPassword: string
}

interface ResetPasswordDialogProps {
  user: User | null
  onClose: () => void
}

export function ResetPasswordDialog({ user, onClose }: ResetPasswordDialogProps) {
  const { showToast } = useToast()
  const resetPassword = useResetUserPassword()
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ResetPasswordFormValues>({ defaultValues: { newPassword: '' } })

  useEffect(() => {
    if (user) reset({ newPassword: '' })
  }, [user, reset])

  const handleClose = () => {
    if (resetPassword.isPending) return
    onClose()
  }

  const onSubmit = async (values: ResetPasswordFormValues) => {
    if (!user) return
    try {
      await resetPassword.mutateAsync({ id: user.id, newPassword: values.newPassword })
      showToast(`Password for ${user.firstName} ${user.lastName} was reset.`)
      onClose()
    } catch (error) {
      showToast(getErrorMessage(error, 'Could not reset this password.'), 'error')
    }
  }

  return (
    <Dialog
      open={user !== null}
      onClose={handleClose}
      title="Reset password"
      description={
        user ? `Set a new temporary password for ${user.firstName} ${user.lastName}. Share it securely.` : undefined
      }
    >
      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4" noValidate>
        <TextField
          label="New temporary password"
          type="password"
          autoComplete="new-password"
          maxLength={USER_PASSWORD_MAX_LENGTH}
          hint="Use at least 8 characters."
          error={errors.newPassword?.message}
          {...register('newPassword', { validate: validatePassword })}
        />
        <div className="mt-2 flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={handleClose} disabled={resetPassword.isPending}>
            Cancel
          </Button>
          <Button type="submit" isLoading={resetPassword.isPending}>
            Reset password
          </Button>
        </div>
      </form>
    </Dialog>
  )
}
