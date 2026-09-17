import { useState } from 'react'
import { Dialog } from '@/components/common/Dialog'
import { useToast } from '@/components/common/useToast'
import { UserForm } from '@/features/users/components/UserForm'
import type { UserFormValues } from '@/features/users/components/UserForm'
import { useCreateUser } from '@/features/users/hooks/useCreateUser'
import { getErrorMessage } from '@/lib/problemDetails'

interface UserCreateDialogProps {
  open: boolean
  onClose: () => void
}

export function UserCreateDialog({ open, onClose }: UserCreateDialogProps) {
  const { showToast } = useToast()
  const createUser = useCreateUser()
  const [submitError, setSubmitError] = useState<string | null>(null)

  const handleClose = () => {
    if (createUser.isPending) return
    setSubmitError(null)
    onClose()
  }

  const handleSubmit = async (values: UserFormValues) => {
    setSubmitError(null)
    try {
      const user = await createUser.mutateAsync({
        firstName: values.firstName,
        lastName: values.lastName,
        email: values.email,
        password: values.password ?? '',
        role: values.role,
      })
      showToast(`${user.firstName} ${user.lastName} was created.`)
      setSubmitError(null)
      onClose()
    } catch (error) {
      setSubmitError(getErrorMessage(error, 'Could not create this user.'))
    }
  }

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      size="lg"
      title="New web user"
      description="Create a Backoffice or Grid Operator account with a temporary password."
    >
      <UserForm
        mode="create"
        isSubmitting={createUser.isPending}
        submitError={submitError}
        onSubmit={handleSubmit}
        onCancel={handleClose}
      />
    </Dialog>
  )
}
