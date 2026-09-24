import { formatStorageKwh } from '@/features/microgrid/format'
import { BarChart, ChartCard, DonutChart } from '@/features/analytics/components/charts'
import { NodeAnalyticsTable } from '@/features/analytics/components/NodeAnalyticsTable'
import { NodeMap } from '@/features/analytics/components/NodeMap'
import { chartColors } from '@/features/analytics/palette'
import type { AnalyticsSnapshot } from '@/features/analytics/types'

export function NodesTab({ snapshot }: { snapshot: AnalyticsSnapshot }) {
  const { network, nodes } = snapshot
  const busiest = [...nodes]
    .sort((a, b) => b.activeReservations - a.activeReservations || b.utilizationPercent - a.utilizationPercent)
    .slice(0, 6)
  const capacity = [...nodes]
    .sort((a, b) => b.totalBatteryKwh - a.totalBatteryKwh)
    .slice(0, 6)

  return (
    <div className="space-y-6">
      <div className="grid gap-4 lg:grid-cols-[minmax(0,20rem)_1fr]">
        <ChartCard title="Node status" description="Active hubs versus deactivated ones.">
          <DonutChart
            centerValue={network.nodeCount}
            centerLabel="nodes"
            slices={[
              { label: 'Active', value: network.activeNodes, color: chartColors.brand },
              { label: 'Inactive', value: network.inactiveNodes, color: chartColors.ink },
            ]}
          />
        </ChartCard>
        <ChartCard title="Busiest nodes" description="Live pending and approved bookings per node.">
          {busiest.length === 0 ? (
            <p className="py-6 text-center text-sm text-ink-500">No nodes registered yet.</p>
          ) : (
            <BarChart
              bars={busiest.map((node) => ({
                label: node.name,
                value: node.activeReservations,
                color: node.deactivationBlocked ? chartColors.amberSoft : chartColors.brand,
                hint: `${node.utilizationPercent}% of slots in use`,
              }))}
            />
          )}
        </ChartCard>
      </div>

      <NodeMap nodes={nodes} />

      {capacity.length > 0 ? (
        <ChartCard title="Battery capacity" description="Installed storage per node, largest first.">
          <BarChart
            unit=" kWh"
            bars={capacity.map((node) => ({
              label: node.name,
              value: Number(node.totalBatteryKwh),
              color: chartColors.brandSoft,
              hint: `${formatStorageKwh(node.committedBatteryKwh)} currently held`,
            }))}
          />
        </ChartCard>
      ) : null}

      <NodeAnalyticsTable nodes={nodes} />
    </div>
  )
}
