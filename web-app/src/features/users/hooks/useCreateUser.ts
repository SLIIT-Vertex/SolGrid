import { useMutation, useQueryClient } from '@tanstack/react-query'
import * as usersApi from '@/features/users/api'
import { usersKeys } from '@/features/users/queryKeys'

export function useCreateUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: usersApi.createUser,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: usersKeys.lists() })
    },
  })
}
