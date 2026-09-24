import { BarChart, ChartCard, DonutChart } from '@/features/analytics/components/charts'
import { KpiStrip } from '@/features/analytics/components/KpiStrip'
import { chartColors } from '@/features/analytics/palette'
import type { AccountAnalytics } from '@/features/analytics/types'

export function AccountsTab({ accounts }: { accounts: AccountAnalytics }) {
  return (
    <div className="space-y-6">
      <KpiStrip
        items={[
          { label: 'Web users', value: accounts.webUsers, hint: `${accounts.activeWebUsers} active` },
          { label: 'Backoffice', value: accounts.backofficeUsers },
          { label: 'Grid Operators', value: accounts.gridOperatorUsers },
          { label: 'Prosumers', value: accounts.prosumers },
          {
            label: 'Awaiting activation',
            value: accounts.pendingProsumers,
            tone: accounts.pendingProsumers > 0 ? 'attention' : 'default',
          },
        ]}
      />

      <div className="grid gap-4 lg:grid-cols-2">
        <ChartCard title="Prosumer accounts" description="Registrations by lifecycle state.">
          <DonutChart
            centerValue={accounts.prosumers}
            centerLabel="prosumers"
            slices={[
              { label: 'Active', value: accounts.activeProsumers, color: chartColors.brand },
              { label: 'Pending', value: accounts.pendingProsumers, color: chartColors.amberSoft },
              { label: 'Deactivation requested', value: accounts.deactivationRequested, color: chartColors.amber },
              { label: 'Deactivated', value: accounts.deactivatedProsumers, color: chartColors.ink },
            ]}
          />
        </ChartCard>
        <ChartCard title="Web console users" description="Staff accounts by role.">
          <BarChart
            bars={[
              { label: 'Backoffice', value: accounts.backofficeUsers, color: chartColors.brand },
              { label: 'Grid Operators', value: accounts.gridOperatorUsers, color: chartColors.brandSoft },
              { label: 'Active total', value: accounts.activeWebUsers, color: chartColors.ink },
            ]}
          />
        </ChartCard>
      </div>
    </div>
  )
}
