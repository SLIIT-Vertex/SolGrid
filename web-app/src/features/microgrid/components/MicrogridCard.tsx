import { Link } from 'react-router-dom'
import { NodeStatusBadge } from '@/features/microgrid/components/NodeStatusBadge'
import { formatGenerationKw, formatSlotCounts } from '@/features/microgrid/format'
import type { MicrogridNode } from '@/features/microgrid/types'

interface MicrogridCardListProps {
  nodes: MicrogridNode[]
}

export function MicrogridCardList({ nodes }: MicrogridCardListProps) {
  return (
    <ul className="flex flex-col gap-3 p-4">
      {nodes.map((node) => (
        <li key={node.id}>
          <article className="rounded-xl border border-ink-100 bg-white p-4">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <h3 className="truncate font-medium text-ink-900">{node.name}</h3>
                <p className="mt-0.5 font-mono text-xs text-ink-500">{node.code}</p>
              </div>
              <NodeStatusBadge status={node.status} />
            </div>
            <p className="mt-3 text-sm text-ink-600">{node.addressLine}</p>
            <dl className="mt-3 grid grid-cols-2 gap-2 text-sm">
              <div>
                <dt className="text-ink-400">Generation</dt>
                <dd className="font-medium text-ink-800">{formatGenerationKw(node.capacityKw)}</dd>
              </div>
              <div>
                <dt className="text-ink-400">Slots</dt>
                <dd className="font-medium text-ink-800">
                  {formatSlotCounts(node.availableSlotCount, node.totalSlotCount)}
                </dd>
              </div>
            </dl>
            <div className="mt-4 flex justify-end">
              <Link
                to={`/microgrid/${node.id}`}
                className="text-sm font-medium text-brand-600 hover:text-brand-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600"
              >
                View Node →
              </Link>
            </div>
          </article>
        </li>
      ))}
    </ul>
  )
}
