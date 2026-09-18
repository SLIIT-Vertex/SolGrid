import { DashboardSection } from '@/features/dashboard/components/DashboardSection'
import { StatCard } from '@/features/dashboard/components/StatCard'
import { useMicrogridNodes } from '@/features/microgrid/hooks/useMicrogridNodes'
import { defaultMicrogridNodeFilters } from '@/features/microgrid/types'

/**
 * Totals come from the stations list endpoint, which both roles may call. Single-row pages are
 * requested because only `totalCount` is used — summing a page would undercount past page one.
 */
export function MicrogridMetrics() {
  const allNodes = useMicrogridNodes({ ...defaultMicrogridNodeFilters, pageSize: 1 })
  const activeNodes = useMicrogridNodes({ ...defaultMicrogridNodeFilters, status: 'Active', pageSize: 1 })
  const openNodes = useMicrogridNodes({
    ...defaultMicrogridNodeFilters,
    hasAvailableSlots: 'available',
    pageSize: 1,
  })

  return (
    <DashboardSection title="Microgrid" description="Solar stations and their battery slot availability.">
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-3">
        <StatCard
          label="Nodes"
          value={allNodes.data?.totalCount}
          hint="Registered stations"
          to="/microgrid"
          isLoading={allNodes.isLoading}
          isError={allNodes.isError}
        />
        <StatCard
          label="Active nodes"
          value={activeNodes.data?.totalCount}
          hint="Accepting bookings"
          to="/microgrid"
          isLoading={activeNodes.isLoading}
          isError={activeNodes.isError}
        />
        <StatCard
          label="Nodes with open slots"
          value={openNodes.data?.totalCount}
          hint="At least one slot free"
          to="/microgrid"
          isLoading={openNodes.isLoading}
          isError={openNodes.isError}
        />
      </div>
    </DashboardSection>
  )
}
