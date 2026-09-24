import { Fragment, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { NodeStatusBadge } from '@/features/microgrid/components/NodeStatusBadge'
import { formatCoordinate, formatGenerationKw, formatStorageKwh } from '@/features/microgrid/format'
import type { NodeAnalytics } from '@/features/analytics/types'

const reservationLabels = [
  ['Pending', 'pendingReservations'],
  ['Approved', 'approvedReservations'],
  ['Rejected', 'rejectedReservations'],
  ['Cancelled', 'cancelledReservations'],
  ['Completed', 'completedReservations'],
] as const

export function NodeAnalyticsTable({ nodes }: { nodes: NodeAnalytics[] }) {
  const [query, setQuery] = useState('')
  const [openId, setOpenId] = useState<string | null>(null)
  const visible = useMemo(() => {
    const text = query.trim().toLowerCase()
    if (!text) return nodes
    return nodes.filter((node) =>
      [node.name, node.code, node.addressLine].some((value) => value.toLowerCase().includes(text)),
    )
  }, [nodes, query])

  return (
    <section className="space-y-3">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 className="text-sm font-semibold text-ink-900">Every node</h2>
          <p className="text-sm text-ink-500">
            Capacity, battery slots, and reservations for each microgrid hub.
          </p>
        </div>
        <label className="block text-sm text-ink-600">
          <span className="sr-only">Search nodes</span>
          <input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search name, code, or address"
            className="w-full rounded-lg border border-ink-200 bg-white px-3 py-2 text-sm text-ink-900 placeholder:text-ink-400 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600 sm:w-72"
          />
        </label>
      </div>

      {visible.length === 0 ? (
        <p className="rounded-2xl border border-ink-100 bg-white px-4 py-10 text-center text-sm text-ink-500">
          {nodes.length === 0 ? 'No microgrid nodes have been registered yet.' : 'No nodes match that search.'}
        </p>
      ) : (
        <div className="overflow-hidden rounded-2xl border border-ink-100 bg-white">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[880px] text-left text-sm">
              <caption className="sr-only">Analytics for every microgrid node</caption>
              <thead>
                <tr className="border-b border-ink-100 text-xs uppercase tracking-wide text-ink-400">
                  <th scope="col" className="px-4 py-3 font-medium">Node</th>
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="px-4 py-3 font-medium">Capacity</th>
                  <th scope="col" className="px-4 py-3 font-medium">Slots free</th>
                  <th scope="col" className="px-4 py-3 font-medium">Battery held</th>
                  <th scope="col" className="px-4 py-3 font-medium">Live bookings</th>
                  <th scope="col" className="px-4 py-3 font-medium">Use</th>
                </tr>
              </thead>
              <tbody>
                {visible.map((node) => {
                  const open = openId === node.id
                  return (
                    <Fragment key={node.id}>
                      <tr className="border-t border-ink-100">
                        <th scope="row" className="px-4 py-3 text-left font-medium">
                          <button
                            type="button"
                            aria-expanded={open}
                            onClick={() => setOpenId(open ? null : node.id)}
                            className="text-ink-900 hover:text-brand-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600"
                          >
                            {node.name}
                          </button>
                          <p className="mt-0.5 font-mono text-xs font-normal text-ink-500">{node.code}</p>
                        </th>
                        <td className="px-4 py-3"><NodeStatusBadge status={node.status} /></td>
                        <td className="px-4 py-3 tabular-nums text-ink-700">{formatGenerationKw(node.capacityKw)}</td>
                        <td className="px-4 py-3 tabular-nums text-ink-700">{node.availableSlots} / {node.totalSlots}</td>
                        <td className="px-4 py-3 tabular-nums text-ink-700">{formatStorageKwh(node.committedBatteryKwh)}</td>
                        <td className="px-4 py-3 tabular-nums text-ink-700">{node.activeReservations}</td>
                        <td className="px-4 py-3 tabular-nums text-ink-700">{node.utilizationPercent}%</td>
                      </tr>
                      {open ? (
                        <tr className="border-t border-ink-100 bg-ink-50/70">
                          <td colSpan={7} className="px-4 py-4">
                            <NodeDetail node={node} />
                          </td>
                        </tr>
                      ) : null}
                    </Fragment>
                  )
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </section>
  )
}

function NodeDetail({ node }: { node: NodeAnalytics }) {
  return (
    <div className="grid gap-4 text-sm lg:grid-cols-3">
      <div>
        <p className="font-medium text-ink-900">Place</p>
        <p className="mt-1 text-ink-800">{node.addressLine}</p>
        <p className="mt-1 font-mono text-xs text-ink-500">
          {formatCoordinate(node.latitude)}, {formatCoordinate(node.longitude)}
        </p>
        <p className="mt-2 text-ink-600">{node.scheduleDayCount} scheduled weekdays</p>
        <Link
          to={`/microgrid/${node.id}`}
          className="mt-3 inline-block font-medium text-brand-700 hover:text-brand-800 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600"
        >
          Open node
        </Link>
      </div>
      <dl className="grid grid-cols-2 gap-x-4 gap-y-1 text-ink-700">
        <dt>Available</dt><dd className="text-right tabular-nums">{node.availableSlots}</dd>
        <dt>Reserved</dt><dd className="text-right tabular-nums">{node.reservedSlots}</dd>
        <dt>Occupied</dt><dd className="text-right tabular-nums">{node.occupiedSlots}</dd>
        <dt>Out of service</dt><dd className="text-right tabular-nums">{node.outOfServiceSlots}</dd>
        <dt>Battery installed</dt><dd className="text-right tabular-nums">{formatStorageKwh(node.totalBatteryKwh)}</dd>
        <dt>Slot drift</dt><dd className="text-right tabular-nums">{node.slotStateDrift}</dd>
      </dl>
      <div>
        <p className="font-medium text-ink-900">Reservations</p>
        <dl className="mt-1 grid grid-cols-2 gap-x-4 gap-y-1 text-ink-700">
          {reservationLabels.map(([label, key]) => (
            <div key={label} className="contents">
              <dt>{label}</dt>
              <dd className="text-right tabular-nums">{node[key]}</dd>
            </div>
          ))}
        </dl>
        <p className="mt-2 text-ink-600">
          {node.deactivationBlocked
            ? 'Deactivation is blocked while live reservations exist.'
            : 'This node can be deactivated.'}
        </p>
      </div>
    </div>
  )
}
