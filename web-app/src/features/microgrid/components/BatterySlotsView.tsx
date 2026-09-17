import type { ReactNode } from 'react'
import { Button } from '@/components/common/Button'
import { EmptyState } from '@/components/common/QueryStates'
import { StatusBadge } from '@/components/common/StatusBadge'
import { SlotStatusBadge } from '@/features/microgrid/components/SlotStatusBadge'
import { formatDateTimeRange, formatStorageKwh } from '@/features/microgrid/format'
import { isCommittedSlot } from '@/features/microgrid/types'
import type { MicrogridBatterySlot } from '@/features/microgrid/types'

interface BatterySlotsViewProps {
  slots: MicrogridBatterySlot[]
  canManage?: boolean
  emptyTitle?: string
  emptyDescription?: string
  emptyAction?: ReactNode
  onEdit?: (slot: MicrogridBatterySlot) => void
  onActivate?: (slot: MicrogridBatterySlot) => void
  onDeactivate?: (slot: MicrogridBatterySlot) => void
}

export function BatterySlotsView({
  slots,
  canManage = false,
  emptyTitle = 'No battery slots on this node.',
  emptyDescription = 'Slot capacity is measured in kWh.',
  emptyAction,
  onEdit,
  onActivate,
  onDeactivate,
}: BatterySlotsViewProps) {
  if (slots.length === 0) {
    return <EmptyState title={emptyTitle} description={emptyDescription} action={emptyAction} />
  }

  return (
    <>
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full min-w-[760px] text-left text-sm">
          <caption className="sr-only">Battery slots</caption>
          <thead>
            <tr className="border-b border-ink-100 text-xs uppercase tracking-wide text-ink-400">
              <th scope="col" className="px-4 py-3 font-medium">Slot</th>
              <th scope="col" className="px-4 py-3 font-medium">Capacity</th>
              <th scope="col" className="px-4 py-3 font-medium">Booking window</th>
              <th scope="col" className="px-4 py-3 font-medium">Availability</th>
              <th scope="col" className="px-4 py-3 font-medium">Status</th>
              {canManage ? (
                <th scope="col" className="px-4 py-3 font-medium text-right">
                  Actions
                </th>
              ) : null}
            </tr>
          </thead>
          <tbody className="divide-y divide-ink-100">
            {slots.map((slot) => (
              <tr key={slot.id} className="hover:bg-ink-50/60">
                <td className="px-4 py-3 font-medium text-ink-900">#{slot.slotNumber}</td>
                <td className="px-4 py-3 text-ink-700">{formatStorageKwh(slot.batteryCapacityKwh)}</td>
                <td className="px-4 py-3 text-ink-700">{formatDateTimeRange(slot.startTime, slot.endTime)}</td>
                <td className="px-4 py-3">
                  <SlotStatusBadge status={slot.status} />
                </td>
                <td className="px-4 py-3">
                  <StatusBadge
                    label={slot.isActive ? 'Active' : 'Inactive'}
                    tone={slot.isActive ? 'brand' : 'red'}
                  />
                </td>
                {canManage ? (
                  <td className="px-4 py-3">
                    <SlotActions
                      slot={slot}
                      onEdit={onEdit}
                      onActivate={onActivate}
                      onDeactivate={onDeactivate}
                      align="end"
                    />
                  </td>
                ) : null}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ul className="flex flex-col gap-3 p-4 md:hidden">
        {slots.map((slot) => (
          <li key={slot.id} className="rounded-xl border border-ink-100 bg-white p-4">
            <div className="flex items-start justify-between gap-3">
              <p className="font-medium text-ink-900">Slot #{slot.slotNumber}</p>
              <SlotStatusBadge status={slot.status} />
            </div>
            <dl className="mt-3 grid gap-2 text-sm">
              <div>
                <dt className="text-ink-400">Capacity</dt>
                <dd className="font-medium text-ink-800">{formatStorageKwh(slot.batteryCapacityKwh)}</dd>
              </div>
              <div>
                <dt className="text-ink-400">Booking window</dt>
                <dd className="font-medium text-ink-800">{formatDateTimeRange(slot.startTime, slot.endTime)}</dd>
              </div>
              <div>
                <dt className="text-ink-400">Status</dt>
                <dd className="mt-1">
                  <StatusBadge
                    label={slot.isActive ? 'Active' : 'Inactive'}
                    tone={slot.isActive ? 'brand' : 'red'}
                  />
                </dd>
              </div>
            </dl>
            {canManage ? (
              <div className="mt-4">
                <SlotActions slot={slot} onEdit={onEdit} onActivate={onActivate} onDeactivate={onDeactivate} />
              </div>
            ) : null}
          </li>
        ))}
      </ul>
    </>
  )
}

function SlotActions({
  slot,
  align = 'start',
  onEdit,
  onActivate,
  onDeactivate,
}: {
  slot: MicrogridBatterySlot
  align?: 'start' | 'end'
  onEdit?: (slot: MicrogridBatterySlot) => void
  onActivate?: (slot: MicrogridBatterySlot) => void
  onDeactivate?: (slot: MicrogridBatterySlot) => void
}) {
  const canEdit = !isCommittedSlot(slot.status)
  const canActivate = slot.status === 'OutOfService'
  const canDeactivate = slot.status === 'Available'

  if (!canEdit && !canActivate && !canDeactivate) {
    return <p className={align === 'end' ? 'text-right text-xs text-ink-400' : 'text-xs text-ink-400'}>Reservation-owned</p>
  }

  return (
    <div className={align === 'end' ? 'flex justify-end gap-2' : 'flex flex-wrap gap-2'}>
      {canEdit && onEdit ? (
        <Button type="button" variant="secondary" size="sm" onClick={() => onEdit?.(slot)}>
          Edit
        </Button>
      ) : null}
      {canDeactivate ? (
        <Button type="button" variant="danger" size="sm" onClick={() => onDeactivate?.(slot)}>
          Deactivate
        </Button>
      ) : null}
      {canActivate ? (
        <Button type="button" variant="secondary" size="sm" onClick={() => onActivate?.(slot)}>
          Activate
        </Button>
      ) : null}
    </div>
  )
}
