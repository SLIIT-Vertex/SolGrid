import { Link } from 'react-router-dom'
import { NodeStatusBadge } from '@/features/microgrid/components/NodeStatusBadge'
import { formatGenerationKw, formatSlotCounts } from '@/features/microgrid/format'
import type { MicrogridNode } from '@/features/microgrid/types'
import { cn } from '@/lib/cn'

interface MicrogridTableProps {
  nodes: MicrogridNode[]
}

export function MicrogridTable({ nodes }: MicrogridTableProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[720px] text-left text-sm">
        <caption className="sr-only">Microgrid Nodes</caption>
        <thead>
          <tr className="border-b border-ink-100 text-xs uppercase tracking-wide text-ink-400">
            <th scope="col" className="px-4 py-3 font-medium">Node</th>
            <th scope="col" className="px-4 py-3 font-medium">Location</th>
            <th scope="col" className="px-4 py-3 font-medium">Capacity</th>
            <th scope="col" className="px-4 py-3 font-medium">Battery slots</th>
            <th scope="col" className="px-4 py-3 font-medium">Status</th>
            <th scope="col" className="px-4 py-3 font-medium text-right">
              <span className="sr-only">Actions</span>
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {nodes.map((node) => (
            <tr key={node.id} className="hover:bg-ink-50/60">
              <td className="px-4 py-3">
                <Link
                  to={`/microgrid/${node.id}`}
                  className="font-medium text-ink-900 hover:text-brand-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600"
                >
                  {node.name}
                </Link>
                <p className="mt-0.5 font-mono text-xs text-ink-500">{node.code}</p>
              </td>
              <td className="px-4 py-3 text-ink-600">{node.addressLine}</td>
              <td className="px-4 py-3 text-ink-700">{formatGenerationKw(node.capacityKw)}</td>
              <td className="px-4 py-3 text-ink-700">
                {formatSlotCounts(node.availableSlotCount, node.totalSlotCount)}
              </td>
              <td className="px-4 py-3">
                <NodeStatusBadge status={node.status} />
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end">
                  <Link
                    to={`/microgrid/${node.id}`}
                    className={cn(
                      'inline-flex h-8 items-center justify-center rounded-lg border border-ink-200 bg-white px-3 text-sm font-medium text-ink-700 transition-colors hover:bg-ink-50',
                      'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600',
                    )}
                  >
                    View
                  </Link>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
