import { useState } from 'react'
import { Button } from '@/components/common/Button'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { EmptyState, ErrorState, LoadingState } from '@/components/common/QueryStates'
import { Pagination } from '@/components/common/Pagination'
import { useToast } from '@/components/common/useToast'
import { BatterySlotDialog } from '@/features/microgrid/components/BatterySlotDialog'
import { BatterySlotFilters } from '@/features/microgrid/components/BatterySlotFilters'
import { BatterySlotsView } from '@/features/microgrid/components/BatterySlotsView'
import { useActivateMicrogridSlot, useDeactivateMicrogridSlot } from '@/features/microgrid/hooks/useMicrogridSlotMutations'
import { useMicrogridSlots } from '@/features/microgrid/hooks/useMicrogridSlots'
import {
  MAX_SLOTS_PER_STATION,
  defaultMicrogridSlotFilters,
  hasMicrogridSlotFilters,
} from '@/features/microgrid/types'
import type { MicrogridBatterySlot, MicrogridNode, MicrogridSlotFilters } from '@/features/microgrid/types'
import { validateSlotQueryRange } from '@/features/microgrid/validation'
import { getErrorMessage } from '@/lib/problemDetails'

function PlusIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 20 20" fill="currentColor" className={className} aria-hidden="true">
      <path d="M10.75 4.75a.75.75 0 0 0-1.5 0v4.5h-4.5a.75.75 0 0 0 0 1.5h4.5v4.5a.75.75 0 0 0 1.5 0v-4.5h4.5a.75.75 0 0 0 0-1.5h-4.5v-4.5Z" />
    </svg>
  )
}

export function BatterySlotsTab({ node, canManage }: { node: MicrogridNode; canManage: boolean }) {
  const { showToast } = useToast()
  const [filters, setFilters] = useState<MicrogridSlotFilters>(defaultMicrogridSlotFilters)
  const [dialog, setDialog] = useState<{ mode: 'create' | 'edit'; slot?: MicrogridBatterySlot } | null>(null)
  const [pendingAction, setPendingAction] = useState<{
    slot: MicrogridBatterySlot
    kind: 'activate' | 'deactivate'
  } | null>(null)

  const rangeError = validateSlotQueryRange(filters.fromDate, filters.toDate)
  const filtersActive = hasMicrogridSlotFilters(filters)
  const { data, isLoading, isError, refetch } = useMicrogridSlots(node.id, filters, !rangeError)
  const activateSlot = useActivateMicrogridSlot(node.id)
  const deactivateSlot = useDeactivateMicrogridSlot(node.id)

  const canCreate = canManage && node.status === 'Active' && node.totalSlotCount < MAX_SLOTS_PER_STATION
  const isMutating = activateSlot.isPending || deactivateSlot.isPending

  const handleConfirm = async () => {
    if (!pendingAction) return
    try {
      if (pendingAction.kind === 'deactivate') {
        await deactivateSlot.mutateAsync(pendingAction.slot.id)
        showToast(`Battery slot ${pendingAction.slot.slotNumber} was taken out of service.`)
      } else {
        await activateSlot.mutateAsync(pendingAction.slot.id)
        showToast(`Battery slot ${pendingAction.slot.slotNumber} is available again.`)
      }
      setPendingAction(null)
    } catch (error) {
      showToast(getErrorMessage(error, 'Could not change this battery slot.'), 'error')
    }
  }

  return (
    <div>
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-sm font-semibold text-ink-900">Battery slots</h3>
          <p className="mt-1 text-sm text-ink-500">Storage windows and availability for this node.</p>
          {canManage && node.status === 'Inactive' ? (
            <p className="mt-2 text-sm text-ink-500">This node is inactive, so new battery slots cannot be added.</p>
          ) : null}
          {canManage && node.status === 'Active' && node.totalSlotCount >= MAX_SLOTS_PER_STATION ? (
            <p className="mt-2 text-sm text-ink-500">
              A station must not declare more than {MAX_SLOTS_PER_STATION} battery storage slots.
            </p>
          ) : null}
        </div>
        {canCreate ? (
          <Button
            type="button"
            className="w-full sm:w-auto"
            leftIcon={<PlusIcon className="size-4" />}
            onClick={() => setDialog({ mode: 'create' })}
          >
            Add slot
          </Button>
        ) : null}
      </div>

      <div className="overflow-hidden rounded-xl border border-ink-100">
        <div className="border-b border-ink-100 p-4">
          <BatterySlotFilters
            filters={filters}
            rangeError={rangeError}
            onChange={(updater) => setFilters((current) => updater(current))}
          />
        </div>

        {rangeError ? (
          <EmptyState title="Adjust the date range." description={rangeError} />
        ) : isLoading ? (
          <LoadingState label="Loading battery slots…" />
        ) : isError ? (
          <ErrorState message="We couldn't load battery slots." onRetry={() => refetch()} />
        ) : !data || data.items.length === 0 ? (
          <BatterySlotsView
            slots={[]}
            emptyTitle={filtersActive ? 'No battery slots match your filters.' : 'No battery slots on this node.'}
            emptyDescription={
              filtersActive
                ? 'Clear filters and try again.'
                : canCreate
                  ? 'Add a storage window to start taking bookings on this node.'
                  : 'When Backoffice adds battery slots, they will appear here.'
            }
            emptyAction={
              filtersActive ? (
                <Button type="button" variant="secondary" onClick={() => setFilters(defaultMicrogridSlotFilters)}>
                  Clear filters
                </Button>
              ) : undefined
            }
          />
        ) : (
          <>
            <BatterySlotsView
              slots={data.items}
              canManage={canManage}
              onEdit={(slot) => setDialog({ mode: 'edit', slot })}
              onActivate={(slot) => setPendingAction({ slot, kind: 'activate' })}
              onDeactivate={(slot) => setPendingAction({ slot, kind: 'deactivate' })}
            />
            <div className="min-w-0 overflow-x-auto">
              <Pagination
                pageNumber={data.pageNumber}
                pageSize={data.pageSize}
                totalCount={data.totalCount}
                onPageChange={(page) => setFilters((current) => ({ ...current, pageNumber: page }))}
              />
            </div>
          </>
        )}
      </div>

      <BatterySlotDialog
        open={dialog !== null}
        mode={dialog?.mode ?? 'create'}
        node={node}
        slot={dialog?.slot}
        onClose={() => setDialog(null)}
      />

      <ConfirmDialog
        open={pendingAction !== null}
        title={pendingAction?.kind === 'deactivate' ? 'Deactivate battery slot' : 'Activate battery slot'}
        description={
          pendingAction
            ? pendingAction.kind === 'deactivate'
              ? `Slot ${pendingAction.slot.slotNumber} will be taken out of service. Live reservations are checked by the server.`
              : `Slot ${pendingAction.slot.slotNumber} will become available for booking.`
            : undefined
        }
        confirmLabel={pendingAction?.kind === 'deactivate' ? 'Deactivate' : 'Activate'}
        isDestructive={pendingAction?.kind === 'deactivate'}
        isLoading={isMutating}
        onConfirm={handleConfirm}
        onCancel={() => {
          if (isMutating) return
          setPendingAction(null)
        }}
      />
    </div>
  )
}
