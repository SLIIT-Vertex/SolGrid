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

function getFullName(prosumer: Prosumer) {
  return `${prosumer.firstName} ${prosumer.lastName}`
}

function ProsumerActions({
  prosumer,
  onAction,
  onEdit,
  onHistory,
}: {
  prosumer: Prosumer
  onAction: ProsumerTableProps['onAction']
  onEdit?: ProsumerTableProps['onEdit']
  onHistory: ProsumerTableProps['onHistory']
}) {
  const fullName = getFullName(prosumer)

  return (
    <div className="flex flex-wrap items-center justify-end gap-2">
      <Button size="sm" variant="secondary" aria-label={`View history for ${fullName}`} onClick={() => onHistory(prosumer)}>
        History
      </Button>
      {onEdit ? (
        <Button size="sm" variant="secondary" aria-label={`Edit ${fullName}`} onClick={() => onEdit(prosumer)}>
          Edit
        </Button>
      ) : null}
      {prosumer.status === 'Pending' ? (
        <>
          <Button size="sm" aria-label={`Activate ${fullName}`} onClick={() => onAction(prosumer, 'activate')}>
            Activate
          </Button>
          <Button variant="danger" size="sm" aria-label={`Reject ${fullName}`} onClick={() => onAction(prosumer, 'reject')}>
            Reject
          </Button>
        </>
      ) : null}
      {prosumer.status === 'Active' || prosumer.status === 'DeactivationRequested' ? (
        <Button
          variant="danger"
          size="sm"
          aria-label={`Deactivate ${fullName}`}
          onClick={() => onAction(prosumer, 'deactivate')}
        >
          Deactivate
        </Button>
      ) : null}
      {prosumer.status === 'Deactivated' ? (
        <Button
          variant="secondary"
          size="sm"
          aria-label={`Reactivate ${fullName}`}
          onClick={() => onAction(prosumer, 'reactivate')}
        >
          Reactivate
        </Button>
      ) : null}
    </div>
  )
}

export function ProsumerTable({ prosumers, onAction, onEdit, onHistory }: ProsumerTableProps) {
  return (
    <>
      <div className="divide-y divide-ink-100 xl:hidden">
        {prosumers.map((prosumer) => (
          <article key={prosumer.nic} className="space-y-4 p-4">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <h2 className="font-medium text-ink-900">{getFullName(prosumer)}</h2>
                <p className="mt-1 break-all text-sm text-ink-600">{prosumer.email}</p>
              </div>
              <ProsumerStatusBadge status={prosumer.status} />
            </div>
            <dl className="grid grid-cols-2 gap-3 text-sm">
              <div>
                <dt className="text-xs uppercase tracking-wide text-ink-400">NIC</dt>
                <dd className="mt-1 font-mono text-ink-700">{prosumer.nic}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase tracking-wide text-ink-400">Registered</dt>
                <dd className="mt-1 text-ink-600">{dateFormatter.format(new Date(prosumer.createdAt))}</dd>
              </div>
            </dl>
            <ProsumerActions prosumer={prosumer} onAction={onAction} onEdit={onEdit} onHistory={onHistory} />
          </article>
        ))}
      </div>

      <div className="hidden overflow-x-auto xl:block">
        <table className="w-full min-w-[840px] table-fixed text-left text-sm">
          <caption className="sr-only">Prosumer accounts</caption>
          <thead>
            <tr className="border-b border-ink-100 text-xs uppercase tracking-wide text-ink-400">
              <th scope="col" className="w-[21%] px-4 py-3 font-medium">Prosumer</th>
              <th scope="col" className="w-[23%] px-4 py-3 font-medium">Contact</th>
              <th scope="col" className="w-[17%] px-4 py-3 font-medium">Status</th>
              <th scope="col" className="w-[14%] px-4 py-3 font-medium">Registered</th>
              <th scope="col" className="w-[25%] px-4 py-3 font-medium text-right">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-ink-100">
            {prosumers.map((prosumer) => (
              <tr key={prosumer.nic} className="hover:bg-ink-50/60">
                <td className="px-4 py-3">
                  <p className="font-medium text-ink-900">{getFullName(prosumer)}</p>
                  <p className="mt-0.5 font-mono text-xs text-ink-500">{prosumer.nic}</p>
                </td>
                <td className="px-4 py-3 text-ink-600">
                  <p className="truncate" title={prosumer.email}>{prosumer.email}</p>
                  {prosumer.phoneNumber ? <p className="mt-0.5 text-xs text-ink-500">{prosumer.phoneNumber}</p> : null}
                </td>
                <td className="px-4 py-3">
                  <ProsumerStatusBadge status={prosumer.status} />
                </td>
                <td className="px-4 py-3 text-ink-500">{dateFormatter.format(new Date(prosumer.createdAt))}</td>
                <td className="px-4 py-3">
                  <ProsumerActions prosumer={prosumer} onAction={onAction} onEdit={onEdit} onHistory={onHistory} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}
