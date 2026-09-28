import { Button } from '@/components/common/Button'
import { ProsumerStatusBadge } from '@/features/prosumers/components/ProsumerStatusBadge'
import type { Prosumer } from '@/features/prosumers/types'

export type ProsumerAction = 'activate' | 'reject' | 'deactivate' | 'reactivate'

interface ProsumerTableProps {
  prosumers: Prosumer[]
  onHistory: (prosumer: Prosumer) => void
  onEdit?: (prosumer: Prosumer) => void
  onAction: (prosumer: Prosumer, action: ProsumerAction) => void
}

const dateFormatter = new Intl.DateTimeFormat('en-GB', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
})

export function ProsumerTable({ prosumers, onAction, onEdit, onHistory }: ProsumerTableProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-ink-100 text-xs uppercase tracking-wide text-ink-400">
            <th className="px-4 py-3 font-medium">Prosumer</th>
            <th className="px-4 py-3 font-medium">Contact</th>
            <th className="px-4 py-3 font-medium">Status</th>
            <th className="px-4 py-3 font-medium">Registered</th>
            <th className="px-4 py-3 font-medium text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {prosumers.map((prosumer) => {
            const name = `${prosumer.firstName} ${prosumer.lastName}`
            return (
              <tr key={prosumer.nic}>
                <td className="px-4 py-3">
                  <p className="font-medium text-ink-900">{name}</p>
                  <p className="mt-0.5 font-mono text-xs text-ink-500">{prosumer.nic}</p>
                </td>
                <td className="px-4 py-3 text-ink-600">
                  <p>{prosumer.email}</p>
                  {prosumer.phoneNumber ? <p className="mt-0.5 text-xs text-ink-500">{prosumer.phoneNumber}</p> : null}
                </td>
                <td className="px-4 py-3">
                  <ProsumerStatusBadge status={prosumer.status} />
                </td>
                <td className="whitespace-nowrap px-4 py-3 text-ink-500">
                  {dateFormatter.format(new Date(prosumer.createdAt))}
                </td>
                <td className="whitespace-nowrap px-4 py-3 text-right">
                  <div className="inline-flex gap-2">
                    <Button size="sm" variant="secondary" onClick={() => onHistory(prosumer)}>History</Button>
                    {onEdit ? <Button size="sm" variant="secondary" onClick={() => onEdit(prosumer)}>Edit</Button> : null}
                    {prosumer.status === 'Pending' && (
                      <>
                        <Button size="sm" onClick={() => onAction(prosumer, 'activate')}>Activate</Button>
                        <Button variant="danger" size="sm" onClick={() => onAction(prosumer, 'reject')}>Reject</Button>
                      </>
                    )}
                    {(prosumer.status === 'Active' || prosumer.status === 'DeactivationRequested') && (
                      <Button variant="danger" size="sm" onClick={() => onAction(prosumer, 'deactivate')}>Deactivate</Button>
                    )}
                    {prosumer.status === 'Deactivated' && (
                      <Button variant="secondary" size="sm" onClick={() => onAction(prosumer, 'reactivate')}>Reactivate</Button>
                    )}
                  </div>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
