import { useMutation } from '@tanstack/react-query'
import * as usersApi from '@/features/users/api'

export function useResetUserPassword() {
  return useMutation({
    mutationFn: ({ id, newPassword }: { id: string; newPassword: string }) =>
      usersApi.resetUserPassword(id, newPassword),
  })
}
