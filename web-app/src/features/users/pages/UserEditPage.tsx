import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useAuth } from '@/auth/useAuth'
import { ErrorState, LoadingState } from '@/components/common/QueryStates'
import { useToast } from '@/components/common/useToast'
import { UserForm } from '@/features/users/components/UserForm'
import type { UserFormValues } from '@/features/users/components/UserForm'
import { useUser } from '@/features/users/hooks/useUser'
import { useUpdateUser } from '@/features/users/hooks/useUpdateUser'
import { canChangeRole } from '@/features/users/permissions'
import { getErrorMessage } from '@/lib/problemDetails'

export function UserEditPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { showToast } = useToast()
  const { session, updateSessionProfile } = useAuth()
  const { data: user, isLoading, isError, refetch } = useUser(id)
  const updateUser = useUpdateUser(id ?? '')
  const [submitError, setSubmitError] = useState<string | null>(null)

  const handleSubmit = async (values: UserFormValues) => {
    setSubmitError(null)
    try {
      const updatedUser = await updateUser.mutateAsync({
        firstName: values.firstName,
        lastName: values.lastName,
        email: values.email,
        role: values.role,
      })
      updateSessionProfile({
        userId: updatedUser.id,
        firstName: updatedUser.firstName,
        lastName: updatedUser.lastName,
        email: updatedUser.email,
      })
      showToast('User details were updated.')
      navigate('/users')
    } catch (error) {
      setSubmitError(getErrorMessage(error, 'Could not update this user.'))
    }
  }

  return (
    <div className="mx-auto max-w-xl">
      <div className="mb-6">
        <h2 className="text-lg font-semibold text-ink-900">Edit web user</h2>
        <p className="text-sm text-ink-500">Update account details. Status is managed separately.</p>
      </div>
      <div className="rounded-2xl border border-ink-100 bg-white p-6">
        {isLoading ? (
          <LoadingState label="Loading user…" />
        ) : isError || !user ? (
          <ErrorState message="Couldn't load this user." onRetry={() => refetch()} />
        ) : (
          <UserForm
            key={user.id}
            mode="edit"
            defaultValues={user}
            isSubmitting={updateUser.isPending}
            submitError={submitError}
            lockRole={!canChangeRole(session?.userId, user)}
            onSubmit={handleSubmit}
            onCancel={() => navigate('/users')}
          />
        )}
      </div>
    </div>
  )
}
