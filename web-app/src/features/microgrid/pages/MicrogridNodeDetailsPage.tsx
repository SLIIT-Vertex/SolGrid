import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { EmptyState, ErrorState, LoadingState } from '@/components/common/QueryStates'
import { useAuth } from '@/auth/useAuth'
import { BatterySlotsTab } from '@/features/microgrid/components/BatterySlotsTab'
import { MicrogridBackLink } from '@/features/microgrid/components/MicrogridBackLink'
import { MicrogridDetailTabs } from '@/features/microgrid/components/MicrogridDetailTabs'
import { MicrogridSection } from '@/features/microgrid/components/MicrogridSection'
import { NodeStatusBadge } from '@/features/microgrid/components/NodeStatusBadge'
import { NodeStatusActions } from '@/features/microgrid/components/NodeStatusActions'
import { ScheduleTab } from '@/features/microgrid/components/ScheduleTab'
import { useMicrogridNode } from '@/features/microgrid/hooks/useMicrogridNode'
import { canChangeNodeStatus, canEditNode, canManageSchedule, canManageSlots } from '@/features/microgrid/permissions'
import {
  formatCoordinate,
  formatGenerationKw,
  formatScheduleSummary,
  formatSlotCounts,
} from '@/features/microgrid/format'
import type { MicrogridDetailTab, MicrogridNode } from '@/features/microgrid/types'
import { isNotFoundError } from '@/lib/problemDetails'

export function MicrogridNodeDetailsPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { session } = useAuth()
  const { data: node, error, isError, isLoading, refetch } = useMicrogridNode(id)
  const [tab, setTab] = useState<MicrogridDetailTab>('overview')
  const showEdit = canEditNode(session)
  const showStatusActions = canChangeNodeStatus(session)
  const showScheduleEdit = canManageSchedule(session)
  const showSlotCreate = canManageSlots(session)

  if (!id || isNotFoundError(error)) {
    return (
      <div className="mx-auto max-w-5xl">
        <MicrogridBackLink />
        <EmptyState
          title="This Microgrid Node was not found."
          description="It may have been removed, or the link may be incorrect."
          action={
            <Button type="button" variant="secondary" onClick={() => navigate('/microgrid')}>
              Back to Microgrid Nodes
            </Button>
          }
        />
      </div>
    )
  }

  if (isLoading) {
    return (
      <div className="mx-auto max-w-5xl">
        <MicrogridBackLink />
        <LoadingState label="Loading Microgrid Node…" />
      </div>
    )
  }

  if (isError || !node) {
    return (
      <div className="mx-auto max-w-5xl">
        <MicrogridBackLink />
        <ErrorState message="We couldn't load this Microgrid Node." onRetry={() => refetch()} />
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-5xl">
      <MicrogridBackLink />

      <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="truncate text-lg font-semibold text-ink-900">{node.name}</h2>
            <NodeStatusBadge status={node.status} />
          </div>
          <p className="mt-1 font-mono text-sm text-ink-500">{node.code}</p>
          <p className="mt-2 text-sm text-ink-600">{node.addressLine}</p>
        </div>
        {showEdit || showStatusActions ? (
          <div className="flex flex-col gap-2 sm:flex-row">
            {showEdit ? (
              <Button
                type="button"
                variant="secondary"
                className="w-full sm:w-auto"
                onClick={() => navigate(`/microgrid/${node.id}/edit`)}
              >
                Edit Node
              </Button>
            ) : null}
            {showStatusActions ? <NodeStatusActions node={node} /> : null}
          </div>
        ) : null}
      </div>

      <div className="rounded-2xl border border-ink-100 bg-white">
        <div className="px-4 sm:px-6">
          <MicrogridDetailTabs value={tab} onChange={setTab} />
        </div>
        <div
          role="tabpanel"
          id={`microgrid-panel-${tab}`}
          aria-labelledby={`microgrid-tab-${tab}`}
          tabIndex={0}
          className="p-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600 sm:p-6"
        >
          {tab === 'overview' ? <OverviewPanel node={node} /> : null}
          {tab === 'schedule' ? <ScheduleTab node={node} canEdit={showScheduleEdit} /> : null}
          {tab === 'slots' ? <BatterySlotsTab node={node} canManage={showSlotCreate} /> : null}
        </div>
      </div>
    </div>
  )
}

function OverviewPanel({ node }: { node: MicrogridNode }) {
  return (
    <div className="flex flex-col gap-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <MicrogridSection title="Generation">
          <p className="text-2xl font-semibold text-ink-900">{formatGenerationKw(node.capacityKw)}</p>
          <p className="mt-1 text-sm text-ink-500">Installed generation capacity</p>
        </MicrogridSection>
        <MicrogridSection title="Battery slots">
          <p className="text-2xl font-semibold text-ink-900">
            {formatSlotCounts(node.availableSlotCount, node.totalSlotCount)}
          </p>
          <p className="mt-1 text-sm text-ink-500">Available / total</p>
        </MicrogridSection>
      </div>

      <MicrogridSection title="Identity" description="Status and code for this generation node.">
        <dl className="grid gap-3 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-ink-400">Status</dt>
            <dd className="mt-1">
              <NodeStatusBadge status={node.status} />
            </dd>
          </div>
          <div>
            <dt className="text-ink-400">Code</dt>
            <dd className="mt-0.5 font-mono text-ink-700">{node.code}</dd>
          </div>
        </dl>
      </MicrogridSection>

      <MicrogridSection title="Location" description="Address and GPS coordinates for this node.">
        <dl className="grid gap-3 text-sm sm:grid-cols-2">
          <div className="sm:col-span-2">
            <dt className="text-ink-400">Address</dt>
            <dd className="mt-0.5 text-ink-700">{node.addressLine}</dd>
          </div>
          <div>
            <dt className="text-ink-400">Latitude</dt>
            <dd className="mt-0.5 text-ink-700">{formatCoordinate(node.latitude)}</dd>
          </div>
          <div>
            <dt className="text-ink-400">Longitude</dt>
            <dd className="mt-0.5 text-ink-700">{formatCoordinate(node.longitude)}</dd>
          </div>
        </dl>
      </MicrogridSection>

      <MicrogridSection title="Schedule" description="Weekly operating hours at a glance.">
        <p className="text-sm text-ink-700">{formatScheduleSummary(node.schedule)}</p>
      </MicrogridSection>
    </div>
  )
}
