import { useState } from 'react'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { EmptyState, ErrorState, LoadingState } from '@/components/common/QueryStates'
import { Pagination } from '@/components/common/Pagination'
import { useToast } from '@/components/common/useToast'
import { ProsumerFilters } from '@/features/prosumers/components/ProsumerFilters'
import { ProsumerTable } from '@/features/prosumers/components/ProsumerTable'
import type { ProsumerAction } from '@/features/prosumers/components/ProsumerTable'
import { useProsumers } from '@/features/prosumers/hooks/useProsumers'
import {
  useActivateProsumer,
  useDeactivateProsumer,
  useReactivateProsumer,
} from '@/features/prosumers/hooks/useSetProsumerStatus'
import { defaultProsumerFilters } from '@/features/prosumers/types'
import type { Prosumer } from '@/features/prosumers/types'
import { getErrorMessage } from '@/lib/problemDetails'

const actionCopy: Record<
  ProsumerAction,
  { title: string; verb: string; isDestructive?: boolean }
> = {
  activate: { title: 'Activate prosumer', verb: 'activated' },
  deactivate: { title: 'Deactivate prosumer', verb: 'deactivated', isDestructive: true },
  reactivate: { title: 'Reactivate prosumer', verb: 'reactivated' },
}

export function ProsumersListPage() {
  const [filters, setFilters] = useState(defaultProsumerFilters)
  const [pendingAction, setPendingAction] = useState<{ prosumer: Prosumer; action: ProsumerAction } | null>(
    null,
  )

  const { data, isLoading, isError, refetch } = useProsumers(filters)
  const activateProsumer = useActivateProsumer()
  const deactivateProsumer = useDeactivateProsumer()
  const reactivateProsumer = useReactivateProsumer()
  const { showToast } = useToast()

  const isMutating =
    activateProsumer.isPending || deactivateProsumer.isPending || reactivateProsumer.isPending

  const mutationByAction: Record<ProsumerAction, typeof activateProsumer> = {
    activate: activateProsumer,
    deactivate: deactivateProsumer,
    reactivate: reactivateProsumer,
  }

  const handleConfirm = async () => {
    if (!pendingAction) return
    const { prosumer, action } = pendingAction
    try {
      await mutationByAction[action].mutateAsync(prosumer.nic)
      showToast(`${prosumer.firstName} ${prosumer.lastName} was ${actionCopy[action].verb}.`)
      setPendingAction(null)
    } catch (error) {
      showToast(getErrorMessage(error), 'error')
    }
  }

  return (
    <div className="mx-auto max-w-5xl">
      <div className="mb-6">
        <p className="text-sm text-ink-500">
          Manage registered Solar Prosumer accounts. Prosumers self-register from the mobile app.
        </p>
      </div>

      <div className="rounded-2xl border border-ink-100 bg-white">
        <div className="border-b border-ink-100 p-4">
          <ProsumerFilters filters={filters} onChange={setFilters} />
        </div>

        {isLoading ? (
          <LoadingState label="Loading prosumers…" />
        ) : isError ? (
          <ErrorState message="Couldn't load prosumers." onRetry={() => refetch()} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState
            title="No prosumers found"
            description="Try adjusting your filters."
          />
        ) : (
          <>
            <ProsumerTable
              prosumers={data.items}
              onAction={(prosumer, action) => setPendingAction({ prosumer, action })}
            />
            <Pagination
              pageNumber={data.pageNumber}
              pageSize={data.pageSize}
              totalCount={data.totalCount}
              onPageChange={(page) => setFilters((current) => ({ ...current, pageNumber: page }))}
            />
          </>
        )}
      </div>

      <ConfirmDialog
        open={pendingAction !== null}
        title={pendingAction ? actionCopy[pendingAction.action].title : ''}
        description={
          pendingAction
            ? `${pendingAction.prosumer.firstName} ${pendingAction.prosumer.lastName} (${pendingAction.prosumer.nic}) will be ${actionCopy[pendingAction.action].verb}.`
            : undefined
        }
        confirmLabel={pendingAction ? actionCopy[pendingAction.action].title.split(' ')[0] : 'Confirm'}
        isDestructive={pendingAction ? actionCopy[pendingAction.action].isDestructive : false}
        isLoading={isMutating}
        onConfirm={handleConfirm}
        onCancel={() => setPendingAction(null)}
      />
    </div>
  )
}
