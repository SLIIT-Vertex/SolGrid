import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Dialog } from '@/components/common/Dialog'
import { Pagination } from '@/components/common/Pagination'
import { ErrorState, LoadingState } from '@/components/common/QueryStates'
import { getProsumerActivity } from '@/features/prosumers/api'
import { prosumersKeys } from '@/features/prosumers/queryKeys'
import type { Prosumer } from '@/features/prosumers/types'

const labels: Record<string, string> = {
  Registered: 'Account registered', ProfileUpdated: 'Profile updated', Activated: 'Account activated',
  DeactivationRequested: 'Deactivation requested', Deactivated: 'Account deactivated', Reactivated: 'Account reactivated',
}

export function ProsumerHistoryDialog({ prosumer, onClose }: { prosumer: Prosumer; onClose: () => void }) {
  const [page, setPage] = useState(1)
  const { data, isPending, isError, refetch } = useQuery({
    queryKey: [...prosumersKeys.detail(prosumer.nic), 'activity', page],
    queryFn: () => getProsumerActivity(prosumer.nic, page),
  })
  return (
    <Dialog open onClose={onClose} title="Account history" size="lg"
      description={`${prosumer.firstName} ${prosumer.lastName} · ${prosumer.nic}`}>
      {isPending ? <LoadingState label="Loading account history…" />
        : isError ? <ErrorState message="Could not load account history." onRetry={() => refetch()} />
        : data && <>
          {data.items.length === 0 && <p className="text-sm text-ink-500">No recorded activity yet. Older account changes were not recorded.</p>}
          <ol className="divide-y divide-ink-100">
            {data.items.map((event) => <li key={event.version} className="py-4">
              <p className="font-medium text-ink-900">{labels[event.action] ?? event.action}</p>
              <p className="mt-1 text-xs text-ink-500">{new Date(event.occurredAt).toLocaleString()} · {event.actorRole}{event.actorId ? ` (${event.actorId})` : ''}</p>
              {event.reason && <p className="mt-2 whitespace-pre-wrap break-words text-sm text-ink-700">{event.reason}</p>}
            </li>)}
          </ol>
          <Pagination pageNumber={data.pageNumber} pageSize={data.pageSize} totalCount={data.totalCount} onPageChange={setPage} />
        </>}
    </Dialog>
  )
}
