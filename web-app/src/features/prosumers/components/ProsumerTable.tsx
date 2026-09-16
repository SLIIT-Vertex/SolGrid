import { Button } from '@/components/common/Button'
import { ProsumerStatusBadge } from '@/features/prosumers/components/ProsumerStatusBadge'
import type { Prosumer } from '@/features/prosumers/types'

export type ProsumerAction = 'activate' | 'deactivate' | 'reactivate'

interface ProsumerTableProps {
  prosumers: Prosumer[]
  onEdit?: (prosumer: Prosumer) => void
  onAction: (prosumer: Prosumer, action: ProsumerAction) => void
}

const dateFormatter = new Intl.DateTimeFormat('en-GB', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
})

export function ProsumerTable({ prosumers, onAction, onEdit }: ProsumerTableProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[760px] text-left text-sm">
        <thead>
          <tr className="border-b border-ink-100 text-xs uppercase tracking-wide text-ink-400">
            <th className="px-4 py-3 font-medium">NIC</th>
            <th className="px-4 py-3 font-medium">Name</th>
            <th className="px-4 py-3 font-medium">Email</th>
            <th className="px-4 py-3 font-medium">Status</th>
            <th className="px-4 py-3 font-medium">Registered</th>
            <th className="px-4 py-3 font-medium text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {prosumers.map((prosumer) => (
            <tr key={prosumer.nic} className="hover:bg-ink-50/60">
              <td className="px-4 py-3 font-medium text-ink-900">{prosumer.nic}</td>
              <td className="px-4 py-3 text-ink-700">
                {prosumer.firstName} {prosumer.lastName}
              </td>
              <td className="px-4 py-3 text-ink-600">{prosumer.email}</td>
              <td className="px-4 py-3">
                <ProsumerStatusBadge status={prosumer.status} />
              </td>
              <td className="px-4 py-3 text-ink-500">{dateFormatter.format(new Date(prosumer.createdAt))}</td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-2">
                  {onEdit && <Button size="sm" variant="secondary" onClick={() => onEdit(prosumer)}>Edit</Button>}
                  {prosumer.status === 'Pending' ? (
                    <Button size="sm" onClick={() => onAction(prosumer, 'activate')}>
                      Activate
                    </Button>
                  ) : null}
                  {prosumer.status === 'Active' ||
                  prosumer.status === 'DeactivationRequested' ||
                  prosumer.status === 'Pending' ? (
                    <Button variant="danger" size="sm" onClick={() => onAction(prosumer, 'deactivate')}>
                      Deactivate
                    </Button>
                  ) : null}
                  {prosumer.status === 'Deactivated' ? (
                    <Button variant="secondary" size="sm" onClick={() => onAction(prosumer, 'reactivate')}>
                      Reactivate
                    </Button>
                  ) : null}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
