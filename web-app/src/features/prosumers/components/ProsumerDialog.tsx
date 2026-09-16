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
import { getErrorMessage } from '@/lib/problemDetails'

export function ProsumerDialog({ prosumer, onClose }: { prosumer?: Prosumer; onClose: () => void }) {
  const [values, setValues] = useState({ nic: prosumer?.nic ?? '', firstName: prosumer?.firstName ?? '', lastName: prosumer?.lastName ?? '', email: prosumer?.email ?? '', phoneNumber: prosumer?.phoneNumber ?? '', password: '' })
  const client = useQueryClient()
  const { showToast } = useToast()
  const save = useMutation({
    mutationFn: () => prosumer ? updateProsumer(prosumer.nic, values) : createProsumer(values),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: prosumersKeys.all })
      showToast(prosumer ? 'Profile updated.' : 'Prosumer created. Activate the account when ready.')
      onClose()
    },
  })
  const submit = (event: FormEvent) => { event.preventDefault(); save.mutate() }
  const fields = ['nic', 'firstName', 'lastName', 'email', 'phoneNumber', ...(prosumer ? [] : ['password'])] as const
  const labels: Record<string, string> = { nic: 'NIC', firstName: 'First name', lastName: 'Last name', email: 'Email', phoneNumber: 'Phone number', password: 'Password' }
  return <Dialog open onClose={() => { if (!save.isPending) onClose() }} title={prosumer ? 'Edit prosumer' : 'Create prosumer'} size="lg">
    <form onSubmit={submit} className="grid gap-4">
      {fields.map((field) => <TextField key={field} name={field} label={labels[field]} value={values[field as keyof typeof values]} readOnly={field === 'nic' && !!prosumer} required={field !== 'phoneNumber'} type={field === 'password' ? 'password' : field === 'email' ? 'email' : 'text'} onChange={(event) => setValues({ ...values, [field]: event.target.value })} />)}
      {save.isError && <p role="alert" className="text-sm text-red-600">{getErrorMessage(save.error)}</p>}
      <div className="flex justify-end gap-2"><Button type="button" variant="secondary" disabled={save.isPending} onClick={onClose}>Cancel</Button><Button type="submit" disabled={save.isPending}>{save.isPending ? 'Saving…' : 'Save'}</Button></div>
    </form>
  </Dialog>
}
